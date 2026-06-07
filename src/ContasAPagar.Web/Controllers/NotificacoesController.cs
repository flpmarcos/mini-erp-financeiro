using System.Security.Claims;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class NotificacoesController : BaseController
{
    private readonly INotificacaoService _notificacoes;
    public NotificacoesController(INotificacaoService notificacoes) => _notificacoes = notificacoes;

    private IEnumerable<string> Roles => User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    public async Task<IActionResult> Index()
    {
        var itens = await _notificacoes.ListarAsync(UsuarioAtual, Roles, somenteNaoLidas: false, take: 100);
        return View(itens);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLida(int id)
    {
        await _notificacoes.MarcarLidaAsync(id, UsuarioAtual, Roles);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodas()
    {
        await _notificacoes.MarcarTodasLidasAsync(UsuarioAtual, Roles);
        Sucesso("Notificações marcadas como lidas.");
        return RedirectToAction(nameof(Index));
    }
}
