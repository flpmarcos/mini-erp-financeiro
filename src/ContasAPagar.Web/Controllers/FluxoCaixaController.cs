using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class FluxoCaixaController : BaseController
{
    private readonly IFluxoCaixaService _fluxo;
    public FluxoCaixaController(IFluxoCaixaService fluxo) => _fluxo = fluxo;

    public async Task<IActionResult> Index() => View(await _fluxo.ObterAsync(7, 30, 90));
}
