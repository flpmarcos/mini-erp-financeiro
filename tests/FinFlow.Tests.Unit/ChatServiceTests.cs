using FinFlow.Domain.Enums;
using FinFlow.Integrations.Notifications;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinFlow.Tests.Unit;

public class ChatServiceTests
{
    private static (ChatService svc, FinFlow.Data.AppDbContext db, NotificacaoService notif) Build()
    {
        var db = TestSupport.NewDb();
        var notif = new NotificacaoService(db, Array.Empty<INotificationChannel>());
        var svc = new ChatService(db, notif, new Mock<IAuditoriaService>().Object);
        return (svc, db, notif);
    }

    private static NovaConversaVM Conversa(params string[] participantes) => new()
    {
        Titulo = "Teste", Tipo = TipoConversa.Grupo, Participantes = participantes.ToList()
    };

    [Fact]
    public async Task Criar_E_EnviarMensagem_GravaHistorico()
    {
        var (svc, _, _) = Build();
        var conv = (await svc.CriarConversaAsync(Conversa("bob@demo.com"), "ana@demo.com", AreaEmpresa.Financeiro)).Dados!;

        await svc.EnviarMensagemAsync(conv.Id, "ana@demo.com", AreaEmpresa.Financeiro, "Olá time", false);

        var hist = await svc.HistoricoAsync(conv.Id, "ana@demo.com", false);
        hist.Should().ContainSingle(m => m.Texto == "Olá time");
    }

    [Fact]
    public async Task EnviarMensagem_UsuarioForaDaConversa_Bloqueado()
    {
        var (svc, _, _) = Build();
        var conv = (await svc.CriarConversaAsync(Conversa("bob@demo.com"), "ana@demo.com", AreaEmpresa.Financeiro)).Dados!;

        var r = await svc.EnviarMensagemAsync(conv.Id, "intruso@demo.com", AreaEmpresa.TI, "oi", false);
        r.Sucesso.Should().BeFalse();
        (await svc.HistoricoAsync(conv.Id, "intruso@demo.com", false)).Should().BeEmpty();
    }

    [Fact]
    public async Task Mencao_NotificaUsuario()
    {
        var (svc, _, notif) = Build();
        var conv = (await svc.CriarConversaAsync(Conversa("bob@demo.com"), "ana@demo.com", AreaEmpresa.Financeiro)).Dados!;

        await svc.EnviarMensagemAsync(conv.Id, "ana@demo.com", AreaEmpresa.Financeiro, "olha isso @bob@demo.com", false);

        (await notif.ContarNaoLidasAsync("bob@demo.com", Array.Empty<string>())).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConversaVinculadaConta_EhAuditavel_E_AuditorAcessa()
    {
        var (svc, db, _) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new FinFlow.Domain.Entities.ContaPagar
        { Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid, ValorOriginal = 100m, ValorLiquido = 100m, DataVencimento = DateTime.Today };
        db.ContasPagar.Add(conta); await db.SaveChangesAsync();

        var vm = Conversa("ana@demo.com"); vm.ContaPagarId = conta.Id;
        var conv = (await svc.CriarConversaAsync(vm, "financeiro@demo.com", AreaEmpresa.Financeiro)).Dados!;

        conv.EhAuditavel.Should().BeTrue();
        // Auditor não participa, mas acessa por ser auditável.
        (await svc.ParticipaAsync(conv.Id, "auditor@demo.com", ehAuditor: true)).Should().BeTrue();
        (await svc.ParticipaAsync(conv.Id, "auditor@demo.com", ehAuditor: false)).Should().BeFalse();
    }

    [Fact]
    public async Task MensagemVinculadaPagamento_SoftDelete_NaoApagaFisicamente()
    {
        var (svc, db, _) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new FinFlow.Domain.Entities.ContaPagar
        { Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid, ValorOriginal = 100m, ValorLiquido = 100m, DataVencimento = DateTime.Today };
        db.ContasPagar.Add(conta); await db.SaveChangesAsync();

        var vm = Conversa(); vm.ContaPagarId = conta.Id;
        var conv = (await svc.CriarConversaAsync(vm, "ana@demo.com", AreaEmpresa.Financeiro)).Dados!;
        var msg = (await svc.EnviarMensagemAsync(conv.Id, "ana@demo.com", AreaEmpresa.Financeiro, "pagamento ok", false)).Dados!;
        msg.VinculadaPagamento.Should().BeTrue();

        await svc.ExcluirMensagemAsync(msg.Id, "ana@demo.com");

        var noBanco = await db.ChatMensagens.FindAsync(msg.Id);
        noBanco.Should().NotBeNull();            // ainda existe fisicamente
        noBanco!.Excluida.Should().BeTrue();     // soft delete
    }

    [Fact]
    public async Task MarcarLida_RegistraLeitura()
    {
        var (svc, db, _) = Build();
        var conv = (await svc.CriarConversaAsync(Conversa(), "ana@demo.com", AreaEmpresa.Financeiro)).Dados!;
        var msg = (await svc.EnviarMensagemAsync(conv.Id, "ana@demo.com", AreaEmpresa.Financeiro, "oi", false)).Dados!;

        await svc.MarcarLidaAsync(msg.Id, "bob@demo.com");

        (await db.ChatLeituras.AnyAsync(r => r.MessageId == msg.Id && r.Usuario == "bob@demo.com")).Should().BeTrue();
    }
}
