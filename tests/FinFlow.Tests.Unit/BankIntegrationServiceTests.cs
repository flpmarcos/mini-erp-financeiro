using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Integrations.Notifications;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FinFlow.Tests.Unit;

public class BankIntegrationServiceTests
{
    private static async Task<(BankIntegrationService svc, FinFlow.Data.AppDbContext db, ContaPagar conta)> BuildAsync(StatusConta status)
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new ContaPagar
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 1000m, ValorLiquido = 1000m, ValorPago = status == StatusConta.Paga ? 1000m : 0m,
            DataVencimento = DateTime.Today, Status = status
        };
        db.ContasPagar.Add(conta);
        await db.SaveChangesAsync();
        var notif = new NotificacaoService(db, Array.Empty<INotificationChannel>());
        var svc = new BankIntegrationService(db, new Mock<IAuditoriaService>().Object, notif);
        return (svc, db, conta);
    }

    [Fact]
    public async Task Estornar_ContaPaga_RevertePagamento()
    {
        var (svc, db, conta) = await BuildAsync(StatusConta.Paga);
        var r = await svc.EstornarAsync(conta.Id, "duplicidade", "tester");

        r.Sucesso.Should().BeTrue();
        var atual = (await db.ContasPagar.FindAsync(conta.Id))!;
        atual.Status.Should().Be(StatusConta.Estornada);
        atual.ValorPago.Should().Be(0m);
    }

    [Fact]
    public async Task Estornar_SemMotivo_Falha()
    {
        var (svc, _, conta) = await BuildAsync(StatusConta.Paga);
        (await svc.EstornarAsync(conta.Id, "  ", "t")).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Estornar_ContaNaoPaga_Falha()
    {
        var (svc, _, conta) = await BuildAsync(StatusConta.Pendente);
        (await svc.EstornarAsync(conta.Id, "x", "t")).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Webhook_Sucesso_ConfirmaPagamento()
    {
        var (svc, db, conta) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        db.Transacoes.Add(new BankTransaction
        {
            ContaPagarId = conta.Id, Banco = BancoIntegracao.Itau, TipoPagamento = FormaPagamento.Pix,
            Status = StatusTransacaoBancaria.Pendente, Valor = 1000m, CodigoTransacao = "ABC123"
        });
        await db.SaveChangesAsync();

        var r = await svc.ProcessarWebhookAsync("ABC123", StatusTransacaoBancaria.Sucesso, "{}", "webhook");

        r.Sucesso.Should().BeTrue();
        (await db.ContasPagar.FindAsync(conta.Id))!.Status.Should().Be(StatusConta.Paga);
    }

    [Fact]
    public async Task Webhook_TransacaoInexistente_Falha()
    {
        var (svc, _, _) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        (await svc.ProcessarWebhookAsync("NAOEXISTE", StatusTransacaoBancaria.Sucesso, null, "webhook"))
            .Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task ReprocessarPendentes_ConfirmaTransacoes()
    {
        var (svc, db, conta) = await BuildAsync(StatusConta.LiberadaParaPagamento);
        db.Transacoes.Add(new BankTransaction
        {
            ContaPagarId = conta.Id, Banco = BancoIntegracao.Generico, TipoPagamento = FormaPagamento.Ted,
            Status = StatusTransacaoBancaria.Pendente, Valor = 1000m, CodigoTransacao = "PEND1"
        });
        await db.SaveChangesAsync();

        var n = await svc.ReprocessarPendentesAsync("job");

        n.Should().Be(1);
        (await db.ContasPagar.FindAsync(conta.Id))!.Status.Should().Be(StatusConta.Paga);
    }
}
