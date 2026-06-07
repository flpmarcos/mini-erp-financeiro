using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

[Authorize(Policy = Policies.PodeAprovar)]
public class AprovacoesController : BaseController
{
    private readonly IAprovacaoService _aprovacao;
    public AprovacoesController(IAprovacaoService aprovacao) => _aprovacao = aprovacao;

    public async Task<IActionResult> Index()
    {
        var pendentes = await _aprovacao.ListarPendentesAsync();
        return View(pendentes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprovar(DecisaoAprovacaoVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Aprovador)) vm.Aprovador = UsuarioAtual;
        var r = await _aprovacao.AprovarAsync(vm);
        if (r.Sucesso) Sucesso("Conta aprovada e liberada para pagamento."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprovar(DecisaoAprovacaoVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Aprovador)) vm.Aprovador = UsuarioAtual;
        var r = await _aprovacao.ReprovarAsync(vm);
        if (r.Sucesso) Sucesso("Conta reprovada."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Index));
    }
}
