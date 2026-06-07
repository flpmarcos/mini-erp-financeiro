using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

/// <summary>
/// Base das controllers MVC. Exige autenticacao (Identity) e expoe o usuario logado.
/// Controllers que precisam ser publicas (ex.: Account) NAO herdam desta.
/// </summary>
[Authorize]
public abstract class BaseController : Controller
{
    /// <summary>Login do usuario autenticado (usado em auditoria, aprovacoes etc).</summary>
    protected string UsuarioAtual => User.Identity?.Name ?? "sistema";

    /// <summary>Mensagem de sucesso exibida via TempData no proximo request.</summary>
    protected void Sucesso(string msg) => TempData["Sucesso"] = msg;

    /// <summary>Mensagem de erro exibida via TempData no proximo request.</summary>
    protected void Erro(string msg) => TempData["Erro"] = msg;
}
