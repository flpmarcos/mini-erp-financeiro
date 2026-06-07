using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

/// <summary>CRUD simples de categorias, centros de custo e contas bancarias.</summary>
[Authorize(Policy = Policies.PodeCadastrar)]
public class CadastrosController : BaseController
{
    private readonly ICadastroService _service;
    public CadastrosController(ICadastroService service) => _service = service;

    public async Task<IActionResult> Index()
    {
        ViewBag.Categorias = await _service.ListarCategoriasAsync();
        ViewBag.Centros = await _service.ListarCentrosAsync();
        ViewBag.ContasBancarias = await _service.ListarContasBancariasAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarCategoria(Categoria c)
    {
        if (string.IsNullOrWhiteSpace(c.Nome)) Erro("Nome da categoria e obrigatorio.");
        else { await _service.CriarCategoriaAsync(c); Sucesso("Categoria criada."); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarCentro(CentroCusto c)
    {
        if (string.IsNullOrWhiteSpace(c.Nome) || string.IsNullOrWhiteSpace(c.Codigo)) Erro("Codigo e nome sao obrigatorios.");
        else { await _service.CriarCentroAsync(c); Sucesso("Centro de custo criado."); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarContaBancaria(ContaBancaria c)
    {
        if (string.IsNullOrWhiteSpace(c.Nome) || string.IsNullOrWhiteSpace(c.Banco)) Erro("Nome e banco sao obrigatorios.");
        else { await _service.CriarContaBancariaAsync(c); Sucesso("Conta bancaria criada."); }
        return RedirectToAction(nameof(Index));
    }
}
