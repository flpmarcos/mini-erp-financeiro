using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Integrations.Notifications;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Notificações internas. Persiste o "sininho" e dispara os canais fake.
/// Destinatário pode ser login, perfil (role) ou "*" (todos).
/// </summary>
public class NotificacaoService : INotificacaoService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<INotificationChannel> _canais;

    public NotificacaoService(AppDbContext db, IEnumerable<INotificationChannel> canais)
    {
        _db = db;
        _canais = canais;
    }

    public async Task NotificarAsync(string destinatario, string titulo, string? mensagem = null,
        SeveridadeNotificacao severidade = SeveridadeNotificacao.Info, string? link = null)
    {
        var n = new Notificacao
        {
            Destinatario = destinatario,
            Titulo = titulo,
            Mensagem = mensagem,
            Severidade = severidade,
            Link = link
        };
        _db.Notificacoes.Add(n);
        await _db.SaveChangesAsync();

        foreach (var canal in _canais)
            await canal.EnviarAsync(n);
    }

    public Task<List<Notificacao>> ListarAsync(string usuario, IEnumerable<string> roles, bool somenteNaoLidas, int take = 50)
    {
        var query = Visiveis(usuario, roles);
        if (somenteNaoLidas) query = query.Where(n => !n.Lida);
        return query.OrderByDescending(n => n.CriadoEm).Take(take).ToListAsync();
    }

    public Task<int> ContarNaoLidasAsync(string usuario, IEnumerable<string> roles) =>
        Visiveis(usuario, roles).CountAsync(n => !n.Lida);

    public async Task<OperationResult> MarcarLidaAsync(int id, string usuario, IEnumerable<string> roles)
    {
        var n = await _db.Notificacoes.FindAsync(id);
        if (n is null) return OperationResult.Falha("Notificacao nao encontrada.");
        if (!EhVisivel(n, usuario, roles)) return OperationResult.Falha("Sem permissao para esta notificacao.");
        n.Lida = true;
        n.LidaEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task MarcarTodasLidasAsync(string usuario, IEnumerable<string> roles)
    {
        var naoLidas = await Visiveis(usuario, roles).Where(n => !n.Lida).ToListAsync();
        foreach (var n in naoLidas) { n.Lida = true; n.LidaEm = DateTime.UtcNow; }
        if (naoLidas.Count > 0) await _db.SaveChangesAsync();
    }

    private IQueryable<Notificacao> Visiveis(string usuario, IEnumerable<string> roles)
    {
        var lista = roles.ToList();
        return _db.Notificacoes.Where(n =>
            n.Destinatario == usuario || n.Destinatario == "*" || lista.Contains(n.Destinatario));
    }

    private static bool EhVisivel(Notificacao n, string usuario, IEnumerable<string> roles) =>
        n.Destinatario == usuario || n.Destinatario == "*" || roles.Contains(n.Destinatario);
}
