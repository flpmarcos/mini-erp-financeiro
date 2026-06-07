using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services;
using FluentAssertions;

namespace AccountsPayable.Tests.Unit;

public class FluxoCaixaServiceTests
{
    [Fact]
    public async Task Obter_ConsolidaSaldoEProjeta()
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        db.ContasBancarias.Add(new ContaBancaria { Nome = "Conta", Banco = "BB", SaldoInicial = 10000m });

        // A pagar: 2.000 vencendo em 5 dias (entra na projeção de 7d)
        db.ContasPagar.Add(new ContaPagar
        {
            Descricao = "AP", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 2000m, ValorLiquido = 2000m, DataVencimento = DateTime.Today.AddDays(5),
            Status = StatusConta.Pendente
        });

        // A receber: 3.000 vencendo em 5 dias
        var cliente = new Cliente { RazaoSocial = "Cli", Documento = "11222333000181" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        db.ContasReceber.Add(new ContaReceber
        {
            Descricao = "AR", ClienteId = cliente.Id, Valor = 3000m,
            DataVencimento = DateTime.Today.AddDays(5), Status = StatusReceber.Aberta
        });
        await db.SaveChangesAsync();

        var vm = await new FluxoCaixaService(db).ObterAsync(7);

        vm.SaldoAtual.Should().Be(10000m);              // saldo inicial, nada realizado
        var h = vm.Horizontes.Single();
        h.EntradasPrevistas.Should().Be(3000m);
        h.SaidasPrevistas.Should().Be(2000m);
        h.SaldoProjetado.Should().Be(11000m);           // 10000 + 3000 - 2000
        h.ResultadoPrevisto.Should().Be(1000m);
    }
}
