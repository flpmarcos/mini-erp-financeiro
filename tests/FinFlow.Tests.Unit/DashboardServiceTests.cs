using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Services;
using FluentAssertions;

namespace FinFlow.Tests.Unit;

public class DashboardServiceTests
{
    [Fact]
    public async Task TotalPagoMes_ContaApenasAsBaixasDoMesAtual()
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        var conta = new ContaPagar
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 1000m, ValorLiquido = 1000m, ValorPago = 1000m,
            DataVencimento = DateTime.Today, Status = StatusConta.Paga, NumeroParcela = 1
        };
        // Duas baixas: uma no mês passado (400), uma neste mês (600).
        conta.Baixas.Add(new BaixaPagamento { DataPagamento = DateTime.Today.AddMonths(-1), ValorPago = 400m });
        conta.Baixas.Add(new BaixaPagamento { DataPagamento = DateTime.Today, ValorPago = 600m });
        db.ContasPagar.Add(conta);
        await db.SaveChangesAsync();

        var vm = await new DashboardService(db).ObterAsync();

        // Deve contar só a baixa deste mês (600), não o ValorPago acumulado (1000).
        vm.TotalPagoMes.Should().Be(600m);
    }
}
