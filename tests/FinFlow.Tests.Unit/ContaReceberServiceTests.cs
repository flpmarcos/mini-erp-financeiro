using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using FluentAssertions;
using Moq;

namespace FinFlow.Tests.Unit;

public class ContaReceberServiceTests
{
    private static (ContaReceberService svc, FinFlow.Data.AppDbContext db) Build()
    {
        var db = TestSupport.NewDb();
        return (new ContaReceberService(db, new Mock<IAuditoriaService>().Object), db);
    }

    private static async Task<int> SeedClienteAsync(FinFlow.Data.AppDbContext db)
    {
        var c = new Cliente { RazaoSocial = "Cliente X", Documento = "11222333000181" };
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    private static ContaReceberFormVM Form(int clienteId, decimal valor) => new()
    {
        Descricao = "Fatura", ClienteId = clienteId, Valor = valor,
        DataVencimento = DateTime.Today.AddDays(30)
    };

    [Fact]
    public async Task Criar_Valido_AbreConta()
    {
        var (svc, db) = Build();
        var cid = await SeedClienteAsync(db);
        var r = await svc.CriarAsync(Form(cid, 1000m), "tester");
        r.Sucesso.Should().BeTrue();
        r.Dados!.Status.Should().Be(StatusReceber.Aberta);
    }

    [Fact]
    public async Task Receber_ValorTotal_MarcaRecebida()
    {
        var (svc, db) = Build();
        var cid = await SeedClienteAsync(db);
        var conta = (await svc.CriarAsync(Form(cid, 1000m), "t")).Dados!;

        var r = await svc.ReceberAsync(new RecebimentoVM { ContaReceberId = conta.Id, ValorRecebido = 1000m }, "t");

        r.Sucesso.Should().BeTrue();
        (await svc.ObterAsync(conta.Id))!.Status.Should().Be(StatusReceber.Recebida);
    }

    [Fact]
    public async Task Receber_ValorParcial_MarcaParcialmenteRecebida()
    {
        var (svc, db) = Build();
        var cid = await SeedClienteAsync(db);
        var conta = (await svc.CriarAsync(Form(cid, 1000m), "t")).Dados!;

        await svc.ReceberAsync(new RecebimentoVM { ContaReceberId = conta.Id, ValorRecebido = 400m }, "t");

        var atual = (await svc.ObterAsync(conta.Id))!;
        atual.Status.Should().Be(StatusReceber.ParcialmenteRecebida);
        atual.SaldoAReceber.Should().Be(600m);
    }

    [Fact]
    public async Task AtualizarVencidas_MarcaVencidaEInadimplencia()
    {
        var (svc, db) = Build();
        var cid = await SeedClienteAsync(db);
        var vm = Form(cid, 500m);
        vm.DataVencimento = DateTime.Today.AddDays(-10);
        var conta = (await svc.CriarAsync(vm, "t")).Dados!;

        var n = await svc.AtualizarVencidasAsync();

        n.Should().Be(1);
        (await svc.InadimplentesAsync()).Should().ContainSingle(c => c.Id == conta.Id);
    }

    [Fact]
    public async Task Cancelar_ContaRecebida_Falha()
    {
        var (svc, db) = Build();
        var cid = await SeedClienteAsync(db);
        var conta = (await svc.CriarAsync(Form(cid, 100m), "t")).Dados!;
        await svc.ReceberAsync(new RecebimentoVM { ContaReceberId = conta.Id, ValorRecebido = 100m }, "t");

        var r = await svc.CancelarAsync(conta.Id, "t");
        r.Sucesso.Should().BeFalse();
    }
}
