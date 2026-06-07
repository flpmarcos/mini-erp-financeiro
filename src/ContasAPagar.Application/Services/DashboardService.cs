using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>Agrega indicadores e dados de grafico para o dashboard.</summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardVM> ObterAsync()
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);

        // So contas "reais" (exclui conta-mae de parcelamento, NumeroParcela = 0).
        var contas = _db.ContasPagar.AsNoTracking().Where(c => c.NumeroParcela != 0);

        var vm = new DashboardVM
        {
            TotalAPagarMes = await contas
                .Where(c => c.DataVencimento >= inicioMes && c.DataVencimento <= fimMes
                         && c.Status != StatusConta.Paga && c.Status != StatusConta.Cancelada)
                .SumAsync(c => (decimal?)(c.ValorLiquido - c.ValorPago)) ?? 0m,

            TotalVencido = await contas
                .Where(c => c.Status == StatusConta.Vencida)
                .SumAsync(c => (decimal?)(c.ValorLiquido - c.ValorPago)) ?? 0m,

            TotalPagoMes = await contas
                .Where(c => c.DataPagamento >= inicioMes && c.DataPagamento <= fimMes)
                .SumAsync(c => (decimal?)c.ValorPago) ?? 0m,

            TotalPendente = await contas
                .Where(c => c.Status == StatusConta.Pendente)
                .SumAsync(c => (decimal?)c.ValorLiquido) ?? 0m,

            TotalEmAprovacao = await contas
                .Where(c => c.Status == StatusConta.EmAprovacao)
                .SumAsync(c => (decimal?)c.ValorLiquido) ?? 0m,

            TotalParcialmentePago = await contas
                .Where(c => c.Status == StatusConta.ParcialmentePaga)
                .SumAsync(c => (decimal?)(c.ValorLiquido - c.ValorPago)) ?? 0m,
        };

        vm.PorCategoria = await contas
            .Where(c => c.Status != StatusConta.Cancelada && c.Status != StatusConta.Paga)
            .GroupBy(c => c.Categoria!.Nome)
            .Select(g => new GraficoItem { Rotulo = g.Key, Valor = g.Sum(c => c.ValorLiquido - c.ValorPago) })
            .OrderByDescending(g => g.Valor)
            .ToListAsync();

        vm.PorCentroCusto = await contas
            .Where(c => c.Status != StatusConta.Cancelada && c.Status != StatusConta.Paga)
            .GroupBy(c => c.CentroCusto!.Nome)
            .Select(g => new GraficoItem { Rotulo = g.Key, Valor = g.Sum(c => c.ValorLiquido - c.ValorPago) })
            .OrderByDescending(g => g.Valor)
            .ToListAsync();

        vm.Vencidas = await contas
            .Include(c => c.Fornecedor)
            .Where(c => c.Status == StatusConta.Vencida)
            .OrderBy(c => c.DataVencimento)
            .Take(10)
            .ToListAsync();

        vm.ProximasDoVencimento = await contas
            .Include(c => c.Fornecedor)
            .Where(c => c.DataVencimento >= hoje && c.DataVencimento <= hoje.AddDays(7)
                     && c.Status != StatusConta.Paga && c.Status != StatusConta.Cancelada)
            .OrderBy(c => c.DataVencimento)
            .Take(10)
            .ToListAsync();

        return vm;
    }
}
