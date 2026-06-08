using System.Text;
using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Integrations.Cnab;
using FinFlow.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Módulo CNAB DIDÁTICO (fake): gera arquivo de remessa de pagamentos e processa
/// arquivo de retorno do banco, atualizando o status das contas. Ver CnabLayout.
/// </summary>
public class CnabService : ICnabService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public CnabService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<string> GerarRemessaAsync()
    {
        var contas = await _db.ContasPagar.AsNoTracking()
            .Where(c => c.NumeroParcela != 0
                     && (c.Status == StatusConta.LiberadaParaPagamento
                      || c.Status == StatusConta.Pendente
                      || c.Status == StatusConta.Vencida))
            .OrderBy(c => c.Id)
            .ToListAsync();

        var sb = new StringBuilder();
        foreach (var c in contas)
            sb.AppendLine(CnabLayout.LinhaRemessa(c.Id, c.ValorLiquido - c.ValorPago));
        return sb.ToString();
    }

    public async Task<ResultadoRetornoCnab> ProcessarRetornoAsync(Stream arquivo, string usuario)
    {
        int processados = 0, confirmados = 0, rejeitados = 0, ignorados = 0;

        using var reader = new StreamReader(arquivo);
        string? linha;
        while ((linha = await reader.ReadLineAsync()) is not null)
        {
            if (!CnabLayout.TryParseRetorno(linha, out var contaId, out var valor, out var codigo))
            {
                ignorados++;
                continue;
            }

            var conta = await _db.ContasPagar.FirstOrDefaultAsync(c => c.Id == contaId);
            if (conta is null || conta.Status is StatusConta.Paga or StatusConta.Cancelada)
            {
                ignorados++;
                continue;
            }

            processados++;

            if (codigo == CnabLayout.CodConfirmado)
            {
                conta.ValorPago = conta.ValorLiquido;
                conta.DataPagamento = DateTime.Today;
                conta.Status = StatusConta.Paga;
                _db.Baixas.Add(new BaixaPagamento
                {
                    ContaPagarId = conta.Id,
                    DataPagamento = DateTime.Today,
                    ValorPago = valor,
                    FormaPagamento = FormaPagamento.Boleto,
                    Observacao = "Baixa via retorno CNAB"
                });
                await _auditoria.RegistrarAsync(AcaoAuditoria.Pagamento, nameof(ContaPagar), conta.Id,
                    valorNovo: valor.ToString("F2"), usuario: usuario, motivo: "Retorno CNAB confirmado");
                confirmados++;
            }
            else if (codigo == CnabLayout.CodRejeitado)
            {
                await _auditoria.RegistrarAsync(AcaoAuditoria.Reprovacao, nameof(ContaPagar), conta.Id,
                    usuario: usuario, motivo: "Retorno CNAB rejeitado");
                rejeitados++;
            }
            else
            {
                ignorados++;
                processados--;
            }
        }

        await _db.SaveChangesAsync();
        return new ResultadoRetornoCnab(processados, confirmados, rejeitados, ignorados);
    }
}
