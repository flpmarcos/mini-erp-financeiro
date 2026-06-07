using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Fluxo de caixa: consolida saldos bancários (inicial), realizado (pago/recebido)
/// e projeta o saldo futuro considerando contas a pagar e a receber em aberto.
/// Ignora contas canceladas. A conta-mãe de parcelamento (NumeroParcela=0) é excluída.
/// </summary>
public class FluxoCaixaService : IFluxoCaixaService
{
    private readonly AppDbContext _db;
    public FluxoCaixaService(AppDbContext db) => _db = db;

    public async Task<FluxoCaixaVM> ObterAsync(params int[] horizontesDias)
    {
        if (horizontesDias.Length == 0) horizontesDias = new[] { 7, 30, 90 };
        var hoje = DateTime.Today;

        var saldoInicial = await _db.ContasBancarias.SumAsync(c => (decimal?)c.SaldoInicial) ?? 0m;
        var totalPago = await _db.ContasPagar.SumAsync(c => (decimal?)c.ValorPago) ?? 0m;
        var totalRecebido = await _db.ContasReceber.SumAsync(c => (decimal?)c.ValorRecebido) ?? 0m;

        var saldoAtual = saldoInicial + totalRecebido - totalPago;

        var vm = new FluxoCaixaVM
        {
            SaldoAtual = saldoAtual,
            TotalPagoRealizado = totalPago,
            TotalRecebidoRealizado = totalRecebido
        };

        foreach (var dias in horizontesDias)
        {
            var limite = hoje.AddDays(dias);

            var saidas = await _db.ContasPagar
                .Where(c => c.NumeroParcela != 0
                         && c.Status != StatusConta.Paga && c.Status != StatusConta.Cancelada
                         && c.DataVencimento <= limite)
                .SumAsync(c => (decimal?)(c.ValorLiquido - c.ValorPago)) ?? 0m;

            var entradas = await _db.ContasReceber
                .Where(c => c.Status != StatusReceber.Recebida && c.Status != StatusReceber.Cancelada
                         && c.DataVencimento <= limite)
                .SumAsync(c => (decimal?)(c.Valor - c.ValorRecebido)) ?? 0m;

            vm.Horizontes.Add(new HorizonteFluxo
            {
                Dias = dias,
                EntradasPrevistas = entradas,
                SaidasPrevistas = saidas,
                SaldoProjetado = saldoAtual + entradas - saidas
            });
        }

        return vm;
    }
}
