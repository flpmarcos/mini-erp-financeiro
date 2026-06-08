using FinFlow.Domain.Entities;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinFlow.Controllers;

[Authorize(Policy = Policies.Administrar)]
public class RegrasAprovacaoController : BaseController
{
    private readonly IRegraAprovacaoService _regras;
    private readonly IFornecedorService _fornecedores;
    private readonly ICadastroService _cadastros;

    public RegrasAprovacaoController(IRegraAprovacaoService regras, IFornecedorService fornecedores, ICadastroService cadastros)
    {
        _regras = regras;
        _fornecedores = fornecedores;
        _cadastros = cadastros;
    }

    public async Task<IActionResult> Index() => View(await _regras.ListarAsync());

    public async Task<IActionResult> Create()
    {
        await PopularSelectsAsync();
        return View(new RegraAprovacao());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegraAprovacao r)
    {
        var res = await _regras.CriarAsync(r);
        if (!res.Sucesso) { Erro(res.Erro!); await PopularSelectsAsync(); return View(r); }
        Sucesso("Regra criada.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _regras.ObterAsync(id);
        if (r is null) return NotFound();
        await PopularSelectsAsync();
        return View(r);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RegraAprovacao r)
    {
        var res = await _regras.AtualizarAsync(r);
        if (!res.Sucesso) { Erro(res.Erro!); await PopularSelectsAsync(); return View(r); }
        Sucesso("Regra atualizada.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var res = await _regras.RemoverAsync(id);
        if (res.Sucesso) Sucesso("Regra removida."); else Erro(res.Erro!);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularSelectsAsync()
    {
        ViewBag.Categorias = new SelectList(await _cadastros.ListarCategoriasAsync(), "Id", "Nome");
        ViewBag.Centros = new SelectList(await _cadastros.ListarCentrosAsync(), "Id", "Nome");
        ViewBag.Fornecedores = new SelectList(await _fornecedores.ListarAtivosAsync(), "Id", "RazaoSocial");
    }
}
