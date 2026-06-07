using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class ClientesController : BaseController
{
    private readonly IClienteService _service;
    public ClientesController(IClienteService service) => _service = service;

    public async Task<IActionResult> Index(string? busca, int pagina = 1)
    {
        ViewBag.Busca = busca;
        return View(await _service.ListarAsync(busca, pagina, 10));
    }

    [Authorize(Policy = Policies.PodeCadastrar)]
    public IActionResult Create() => View(new Cliente());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create(Cliente c)
    {
        if (!ModelState.IsValid) return View(c);
        var r = await _service.CriarAsync(c);
        if (!r.Sucesso) { Erro(r.Erro!); return View(c); }
        Sucesso("Cliente criado.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await _service.ObterAsync(id);
        return c is null ? NotFound() : View(c);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(Cliente c)
    {
        if (!ModelState.IsValid) return View(c);
        var r = await _service.AtualizarAsync(c);
        if (!r.Sucesso) { Erro(r.Erro!); return View(c); }
        Sucesso("Cliente atualizado.");
        return RedirectToAction(nameof(Index));
    }
}
