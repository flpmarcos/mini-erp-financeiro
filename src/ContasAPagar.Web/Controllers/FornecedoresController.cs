using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class FornecedoresController : BaseController
{
    private readonly IFornecedorService _service;
    public FornecedoresController(IFornecedorService service) => _service = service;

    public async Task<IActionResult> Index(string? busca, int pagina = 1)
    {
        ViewBag.Busca = busca;
        var resultado = await _service.ListarAsync(busca, pagina, 10);
        return View(resultado);
    }

    [Authorize(Policy = Policies.PodeCadastrar)]
    public IActionResult Create() => View(new Fornecedor());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create(Fornecedor f)
    {
        if (!ModelState.IsValid) return View(f);

        var r = await _service.CriarAsync(f);
        if (!r.Sucesso) { Erro(r.Erro!); return View(f); }

        Sucesso("Fornecedor criado.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(int id)
    {
        var f = await _service.ObterAsync(id);
        return f is null ? NotFound() : View(f);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(Fornecedor f)
    {
        if (!ModelState.IsValid) return View(f);

        var r = await _service.AtualizarAsync(f);
        if (!r.Sucesso) { Erro(r.Erro!); return View(f); }

        Sucesso("Fornecedor atualizado.");
        return RedirectToAction(nameof(Index));
    }
}
