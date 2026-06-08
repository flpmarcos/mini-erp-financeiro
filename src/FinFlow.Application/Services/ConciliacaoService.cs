using System.Globalization;
using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Integrations.Conciliacao;
using FinFlow.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Conciliacao bancaria: importa extrato CSV e casa lancamentos com contas pagas
/// por valor + proximidade de data. Permite tambem conciliacao manual.
/// </summary>
public class ConciliacaoService : IConciliacaoService
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private const int ToleranciaDias = 3;
    private const decimal ToleranciaValor = 0.01m;

    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public ConciliacaoService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<OperationResult<ResultadoImportacao>> ImportarCsvAsync(Stream csv, string usuario)
    {
        var config = new CsvConfiguration(PtBr) { Delimiter = ";", HeaderValidated = null, MissingFieldFound = null };

        List<ExtratoCsvRow> linhas;
        try
        {
            using var reader = new StreamReader(csv);
            using var csvReader = new CsvReader(reader, config);
            csvReader.Context.RegisterClassMap<ExtratoCsvMap>();
            linhas = csvReader.GetRecords<ExtratoCsvRow>().ToList();
        }
        catch (Exception ex)
        {
            return OperationResult<ResultadoImportacao>.Falha($"Falha ao ler CSV: {ex.Message}");
        }

        int importados = 0, conciliados = 0;

        // Candidatas: contas pagas/parciais ainda nao totalmente conciliadas no extrato.
        var contasPagas = await _db.ContasPagar
            .Where(c => c.Status == StatusConta.Paga || c.Status == StatusConta.ParcialmentePaga)
            .ToListAsync();

        foreach (var linha in linhas)
        {
            if (!TryParseLinha(linha, out var data, out var valor)) continue;

            var item = new ExtratoBancarioItem
            {
                Data = data,
                Descricao = linha.Descricao,
                Valor = valor,
                Documento = linha.Documento,
                Banco = linha.Banco,
                Tipo = linha.Tipo,
                Status = StatusConciliacao.NaoConciliado
            };

            // Match automatico: valor igual (tolerancia) e data proxima.
            var valorAbs = Math.Abs(valor);
            var match = contasPagas.FirstOrDefault(c =>
                Math.Abs(c.ValorPago - valorAbs) <= ToleranciaValor &&
                c.DataPagamento.HasValue &&
                Math.Abs((c.DataPagamento.Value.Date - data.Date).Days) <= ToleranciaDias);

            if (match is not null)
            {
                item.Status = StatusConciliacao.Conciliado;
                item.ContaPagarId = match.Id;
                item.DataConciliacao = DateTime.UtcNow;
                contasPagas.Remove(match); // evita conciliar 2x com a mesma conta
                conciliados++;
            }

            _db.ExtratoItens.Add(item);
            importados++;
        }

        await _auditoria.RegistrarAsync(AcaoAuditoria.Conciliacao, nameof(ExtratoBancarioItem), 0,
            valorNovo: $"Importados {importados}, conciliados {conciliados}", usuario: usuario);
        await _db.SaveChangesAsync();

        return OperationResult<ResultadoImportacao>.Ok(new ResultadoImportacao(importados, conciliados));
    }

    public async Task<List<ExtratoBancarioItem>> ListarAsync() =>
        await _db.ExtratoItens.AsNoTracking()
            .Include(e => e.ContaPagar)
            .OrderByDescending(e => e.Data)
            .ToListAsync();

    public async Task<OperationResult> ConciliarManualAsync(int extratoItemId, int contaPagarId, string usuario)
    {
        var item = await _db.ExtratoItens.FindAsync(extratoItemId);
        if (item is null) return OperationResult.Falha("Lancamento do extrato nao encontrado.");

        var conta = await _db.ContasPagar.FindAsync(contaPagarId);
        if (conta is null) return OperationResult.Falha("Conta a pagar nao encontrada.");

        item.Status = StatusConciliacao.Conciliado;
        item.ContaPagarId = conta.Id;
        item.DataConciliacao = DateTime.UtcNow;

        await _auditoria.RegistrarAsync(AcaoAuditoria.Conciliacao, nameof(ExtratoBancarioItem), item.Id,
            valorNovo: $"Conciliado manualmente com conta {conta.Id}", usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private static bool TryParseLinha(ExtratoCsvRow linha, out DateTime data, out decimal valor)
    {
        data = default;
        valor = default;

        var formatos = new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
        if (!DateTime.TryParseExact(linha.Data?.Trim(), formatos, PtBr, DateTimeStyles.None, out data)
            && !DateTime.TryParse(linha.Data, PtBr, DateTimeStyles.None, out data))
            return false;

        var bruto = (linha.Valor ?? string.Empty).Trim().Replace("R$", "").Replace(".", "").Replace(",", ".");
        return decimal.TryParse(bruto, NumberStyles.Any, CultureInfo.InvariantCulture, out valor);
    }
}
