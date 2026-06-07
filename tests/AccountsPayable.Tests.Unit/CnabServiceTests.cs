using System.Text;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Integrations.Cnab;
using ContasAPagar.Web.Services;
using ContasAPagar.Web.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace AccountsPayable.Tests.Unit;

public class CnabServiceTests
{
    private static async Task<(CnabService svc, ContasAPagar.Web.Data.AppDbContext db, ContaPagar conta)> BuildAsync(StatusConta status)
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new ContaPagar
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 1000m, ValorLiquido = 1000m, DataVencimento = DateTime.Today, Status = status
        };
        db.ContasPagar.Add(conta);
        await db.SaveChangesAsync();
        var svc = new CnabService(db, new Mock<IAuditoriaService>().Object);
        return (svc, db, conta);
    }

    [Fact]
    public async Task GerarRemessa_IncluiContaLiberada()
    {
        var (svc, _, conta) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        var remessa = await svc.GerarRemessaAsync();
        remessa.Should().Contain(CnabLayout.LinhaRemessa(conta.Id, 1000m));
    }

    [Fact]
    public async Task ProcessarRetorno_Confirmado_MarcaPaga()
    {
        var (svc, db, conta) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        var linha = $"T{conta.Id:D8}{100000:D13}00"; // 1000,00 confirmado

        var r = await svc.ProcessarRetornoAsync(new MemoryStream(Encoding.UTF8.GetBytes(linha)), "tester");

        r.Confirmados.Should().Be(1);
        (await db.ContasPagar.FindAsync(conta.Id))!.Status.Should().Be(StatusConta.Paga);
    }

    [Fact]
    public async Task ProcessarRetorno_Rejeitado_NaoPaga()
    {
        var (svc, db, conta) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        var linha = $"T{conta.Id:D8}{100000:D13}09";

        var r = await svc.ProcessarRetornoAsync(new MemoryStream(Encoding.UTF8.GetBytes(linha)), "tester");

        r.Rejeitados.Should().Be(1);
        (await db.ContasPagar.FindAsync(conta.Id))!.Status.Should().NotBe(StatusConta.Paga);
    }

    [Fact]
    public async Task ProcessarRetorno_LinhaInvalida_Ignorada()
    {
        var (svc, _, _) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        var r = await svc.ProcessarRetornoAsync(new MemoryStream(Encoding.UTF8.GetBytes("linha-bugada\nXYZ")), "tester");
        r.Ignorados.Should().BeGreaterThan(0);
        r.Confirmados.Should().Be(0);
    }
}
