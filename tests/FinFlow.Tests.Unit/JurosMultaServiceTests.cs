using FinFlow.Domain.Entities;
using FinFlow.Services;
using FluentAssertions;

namespace FinFlow.Tests.Unit;

public class JurosMultaServiceTests
{
    private static ContaPagar Conta(decimal liquido, DateTime vencimento) =>
        new() { ValorLiquido = liquido, ValorOriginal = liquido, DataVencimento = vencimento };

    [Fact]
    public void Calcular_SemAtraso_NaoAplicaEncargos()
    {
        var svc = new JurosMultaService(TestSupport.Financeiro());
        var conta = Conta(1000m, DateTime.Today.AddDays(5));

        var r = svc.Calcular(conta, DateTime.Today);

        r.DiasAtraso.Should().Be(0);
        r.Multa.Should().Be(0m);
        r.Juros.Should().Be(0m);
        r.ValorAtualizado.Should().Be(1000m);
    }

    [Fact]
    public void Calcular_ComAtraso_AplicaMultaUnicaMaisJurosPorDia()
    {
        // multa 2% (= 20), juros 0,033%/dia * 10 dias (= 0,33% = 3,30)
        var svc = new JurosMultaService(TestSupport.Financeiro());
        var conta = Conta(1000m, DateTime.Today.AddDays(-10));

        var r = svc.Calcular(conta, DateTime.Today);

        r.DiasAtraso.Should().Be(10);
        r.Multa.Should().Be(20.00m);
        r.Juros.Should().Be(3.30m);
        r.ValorAtualizado.Should().Be(1023.30m);
    }

    [Fact]
    public void Calcular_UsaSaldoDevedor_QuandoHaPagamentoParcial()
    {
        var svc = new JurosMultaService(TestSupport.Financeiro());
        var conta = Conta(1000m, DateTime.Today.AddDays(-10));
        conta.ValorPago = 600m; // saldo devedor = 400

        var r = svc.Calcular(conta, DateTime.Today);

        r.ValorOriginal.Should().Be(400m);       // base = saldo devedor
        r.Multa.Should().Be(8.00m);              // 2% de 400
        r.ValorAtualizado.Should().Be(400m + r.Multa + r.Juros);
    }
}
