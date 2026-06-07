using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>Relatorios financeiros (consultas agregadas, somente leitura).</summary>
public class RelatorioService : IRelatorioService
{
    private readonly AppDbContext _db;
    public RelatorioService(AppDbContext db) => _db = db;

    private IQueryable<ContaPagar> Base => _db.ContasPagar.AsNoTracking().Where(c => c.NumeroParcela != 0);

    public Task<List<ContaPagar>> ContasVencidasAsync() =>
        Base.Include(c => c.Fornecedor).Where(c => c.Status == StatusConta.Vencida)
            .OrderBy(c => c.DataVencimento).ToListAsync();

    public Task<List<ContaPagar>> AVencer7DiasAsync()
    {
        var hoje = DateTime.Today;
        return Base.Include(c => c.Fornecedor)
            .Where(c => c.DataVencimento >= hoje && c.DataVencimento <= hoje.AddDays(7)
                     && c.Status != StatusConta.Paga && c.Status != StatusConta.Cancelada)
            .OrderBy(c => c.DataVencimento).ToListAsync();
    }

    public Task<List<ContaPagar>> PagasNoMesAsync()
    {
        var hoje = DateTime.Today;
        var inicio = new DateTime(hoje.Year, hoje.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        return Base.Include(c => c.Fornecedor)
            .Where(c => c.DataPagamento >= inicio && c.DataPagamento <= fim)
            .OrderByDescending(c => c.DataPagamento).ToListAsync();
    }

    public Task<List<ContaPagar>> PendentesAsync() =>
        Base.Include(c => c.Fornecedor).Where(c => c.Status == StatusConta.Pendente)
            .OrderBy(c => c.DataVencimento).ToListAsync();

    public Task<List<ContaPagar>> ParcialmentePagasAsync() =>
        Base.Include(c => c.Fornecedor).Where(c => c.Status == StatusConta.ParcialmentePaga)
            .OrderBy(c => c.DataVencimento).ToListAsync();

    public async Task<List<GraficoItem>> FluxoCaixaPrevistoAsync()
    {
        // Agrupa saldo devedor por mes de vencimento (proximos vencimentos).
        var dados = await Base
            .Where(c => c.Status != StatusConta.Paga && c.Status != StatusConta.Cancelada)
            .Select(c => new { c.DataVencimento, Saldo = c.ValorLiquido - c.ValorPago })
            .ToListAsync();

        return dados
            .GroupBy(x => new { x.DataVencimento.Year, x.DataVencimento.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new GraficoItem
            {
                Rotulo = $"{g.Key.Month:00}/{g.Key.Year}",
                Valor = g.Sum(x => x.Saldo)
            }).ToList();
    }

    public Task<List<GraficoItem>> DespesasPorFornecedorAsync() =>
        Base.Where(c => c.Status != StatusConta.Cancelada)
            .GroupBy(c => c.Fornecedor!.RazaoSocial)
            .Select(g => new GraficoItem { Rotulo = g.Key, Valor = g.Sum(c => c.ValorLiquido) })
            .OrderByDescending(g => g.Valor).ToListAsync();

    public Task<List<GraficoItem>> DespesasPorCentroCustoAsync() =>
        Base.Where(c => c.Status != StatusConta.Cancelada)
            .GroupBy(c => c.CentroCusto!.Nome)
            .Select(g => new GraficoItem { Rotulo = g.Key, Valor = g.Sum(c => c.ValorLiquido) })
            .OrderByDescending(g => g.Valor).ToListAsync();

    public Task<List<GraficoItem>> DespesasPorCategoriaAsync() =>
        Base.Where(c => c.Status != StatusConta.Cancelada)
            .GroupBy(c => c.Categoria!.Nome)
            .Select(g => new GraficoItem { Rotulo = g.Key, Valor = g.Sum(c => c.ValorLiquido) })
            .OrderByDescending(g => g.Valor).ToListAsync();

    public Task<List<GraficoItem>> ImpostosRetidosAsync() =>
        _db.Retencoes.AsNoTracking()
            .GroupBy(r => r.Tipo)
            .Select(g => new GraficoItem { Rotulo = g.Key.ToString(), Valor = g.Sum(r => r.Valor) })
            .OrderByDescending(g => g.Valor).ToListAsync();

    public Task<List<GraficoItem>> PagamentosPorBancoAsync() =>
        _db.Transacoes.AsNoTracking()
            .Where(t => t.Status == StatusTransacaoBancaria.Sucesso)
            .GroupBy(t => t.Banco)
            .Select(g => new GraficoItem { Rotulo = g.Key.ToString(), Valor = g.Sum(t => t.Valor) })
            .OrderByDescending(g => g.Valor).ToListAsync();
}
