using System.Security.Claims;
using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.ViewComponents;

/// <summary>Sininho de notificações no topo: contador de não lidas + últimas.</summary>
public class NotificacoesBellViewComponent : ViewComponent
{
    private readonly INotificacaoService _notificacoes;
    public NotificacoesBellViewComponent(INotificacaoService notificacoes) => _notificacoes = notificacoes;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var usuario = User.Identity?.Name ?? string.Empty;
        var roles = ((ClaimsPrincipal)User).FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        ViewBag.NaoLidas = await _notificacoes.ContarNaoLidasAsync(usuario, roles);
        var recentes = await _notificacoes.ListarAsync(usuario, roles, somenteNaoLidas: false, take: 6);
        return View(recentes);
    }
}
