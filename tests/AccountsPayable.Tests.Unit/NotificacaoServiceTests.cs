using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Integrations.Notifications;
using ContasAPagar.Web.Services;
using FluentAssertions;

namespace AccountsPayable.Tests.Unit;

public class NotificacaoServiceTests
{
    private static NotificacaoService Svc() =>
        new(TestSupport.NewDb(), Array.Empty<INotificationChannel>());

    private static NotificacaoService Svc(ContasAPagar.Web.Data.AppDbContext db) =>
        new(db, Array.Empty<INotificationChannel>());

    [Fact]
    public async Task Notificar_CriaNaoLida_VisivelPorRole()
    {
        var db = TestSupport.NewDb();
        var svc = Svc(db);
        await svc.NotificarAsync("Gerente", "Aprovar conta", "msg", SeveridadeNotificacao.Alerta);

        var lista = await svc.ListarAsync("bruno@demo.com", new[] { "Gerente" }, somenteNaoLidas: true);
        lista.Should().ContainSingle(n => n.Titulo == "Aprovar conta");
    }

    [Fact]
    public async Task Listar_NaoVeNotificacaoDeOutroPerfil()
    {
        var db = TestSupport.NewDb();
        var svc = Svc(db);
        await svc.NotificarAsync("Diretor", "Alta alçada");

        var lista = await svc.ListarAsync("ana@demo.com", new[] { "Financeiro" }, somenteNaoLidas: false);
        lista.Should().BeEmpty();
    }

    [Fact]
    public async Task Curinga_VisivelParaTodos()
    {
        var db = TestSupport.NewDb();
        var svc = Svc(db);
        await svc.NotificarAsync("*", "Aviso geral");
        (await svc.ContarNaoLidasAsync("qualquer", new[] { "Auditor" })).Should().Be(1);
    }

    [Fact]
    public async Task MarcarLida_ReduzContador()
    {
        var db = TestSupport.NewDb();
        var svc = Svc(db);
        await svc.NotificarAsync("ana@demo.com", "Para Ana");
        var n = (await svc.ListarAsync("ana@demo.com", Array.Empty<string>(), false)).Single();

        await svc.MarcarLidaAsync(n.Id, "ana@demo.com", Array.Empty<string>());

        (await svc.ContarNaoLidasAsync("ana@demo.com", Array.Empty<string>())).Should().Be(0);
    }
}
