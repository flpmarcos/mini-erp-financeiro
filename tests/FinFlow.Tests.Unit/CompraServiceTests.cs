using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using FluentAssertions;
using Moq;

namespace FinFlow.Tests.Unit;

public class CompraServiceTests
{
    private static async Task<(CompraService svc, FinFlow.Data.AppDbContext db, int id)> NovaSolicitacaoAsync()
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var svc = new CompraService(db, new Mock<IAuditoriaService>().Object);
        var r = await svc.CriarAsync(new CompraFormVM
        {
            Descricao = "Notebooks", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid, ValorEstimado = 5000m
        }, "comprador");
        return (svc, db, r.Dados!.Id);
    }

    [Fact]
    public async Task Criar_NasceSolicitada()
    {
        var (svc, _, id) = await NovaSolicitacaoAsync();
        (await svc.ObterAsync(id))!.Status.Should().Be(StatusCompra.Solicitada);
    }

    [Fact]
    public async Task FluxoCompleto_GeraContaAPagar()
    {
        var (svc, db, id) = await NovaSolicitacaoAsync();

        (await svc.AprovarAsync(id, "gerente")).Sucesso.Should().BeTrue();
        (await svc.EmitirPedidoAsync(id, "comprador")).Sucesso.Should().BeTrue();
        var receb = await svc.ReceberAsync(id, "almox");

        receb.Sucesso.Should().BeTrue();
        var compra = (await svc.ObterAsync(id))!;
        compra.Status.Should().Be(StatusCompra.Recebida);
        compra.ContaPagarGeradaId.Should().Be(receb.Dados!.Id);
        (await db.ContasPagar.FindAsync(receb.Dados.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Receber_SemPedidoEmitido_Falha()
    {
        var (svc, _, id) = await NovaSolicitacaoAsync();
        // ainda Solicitada → não pode receber
        (await svc.ReceberAsync(id, "almox")).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Aprovar_ForaDeOrdem_Falha()
    {
        var (svc, _, id) = await NovaSolicitacaoAsync();
        await svc.AprovarAsync(id, "g");
        // aprovar de novo (status já Aprovada) deve falhar
        (await svc.AprovarAsync(id, "g")).Sucesso.Should().BeFalse();
    }
}
