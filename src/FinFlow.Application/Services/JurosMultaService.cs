using FinFlow.Configurations;
using FinFlow.Domain.Entities;
using FinFlow.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FinFlow.Services;

/// <summary>
/// Regra: conta vencida recebe multa (percentual fixo, uma vez) + juros ao dia.
/// ValorAtualizado = SaldoDevedor + multa + juros. Base = saldo devedor (liquido - pago).
/// </summary>
public class JurosMultaService : IJurosMultaService
{
    private readonly FinanceiroOptions _opt;
    public JurosMultaService(IOptions<FinanceiroOptions> opt) => _opt = opt.Value;

    public CalculoEncargos Calcular(ContaPagar conta, DateTime? referencia = null)
    {
        var hoje = (referencia ?? DateTime.Today).Date;
        var baseCalculo = conta.SaldoDevedor > 0 ? conta.SaldoDevedor : conta.ValorLiquido;

        var diasAtraso = (hoje - conta.DataVencimento.Date).Days;
        if (diasAtraso <= 0)
            return new CalculoEncargos(baseCalculo, 0, 0m, 0m, baseCalculo);

        var multa = Math.Round(baseCalculo * (_opt.MultaPercentual / 100m), 2);
        var juros = Math.Round(baseCalculo * (_opt.JurosDiarioPercentual / 100m) * diasAtraso, 2);
        var atualizado = baseCalculo + multa + juros;

        return new CalculoEncargos(baseCalculo, diasAtraso, multa, juros, atualizado);
    }
}
