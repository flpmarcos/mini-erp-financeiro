using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Controllers;

[Authorize(Policy = Policies.PodeAuditar)]
public class AuditoriaController : BaseController
{
    private readonly IAuditoriaService _auditoria;
    public AuditoriaController(IAuditoriaService auditoria) => _auditoria = auditoria;

    public async Task<IActionResult> Index(string? entidade, int pagina = 1)
    {
        ViewBag.Entidade = entidade;
        var resultado = await _auditoria.ListarAsync(entidade, pagina, 20);
        return View(resultado);
    }
}
