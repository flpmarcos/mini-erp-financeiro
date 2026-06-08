using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;
    private readonly IContaPagarService _contas;

    public DashboardController(IDashboardService dashboard, IContaPagarService contas)
    {
        _dashboard = dashboard;
        _contas = contas;
    }

    public async Task<IActionResult> Index()
    {
        // Reavalia contas vencidas a cada visita ao dashboard (job simples).
        await _contas.AtualizarVencidasAsync();

        var vm = await _dashboard.ObterAsync();
        return View(vm);
    }
}
