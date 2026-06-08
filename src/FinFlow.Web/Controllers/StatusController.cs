using System.Reflection;
using FinFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinFlow.Controllers;

/// <summary>Pagina amigavel de status do sistema (health + ambiente).</summary>
public class StatusController : BaseController
{
    private readonly HealthCheckService _health;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public StatusController(HealthCheckService health, IConfiguration config, IWebHostEnvironment env)
    {
        _health = health;
        _config = config;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var report = await _health.CheckHealthAsync();
        ViewBag.Status = report.Status.ToString();
        ViewBag.Entries = report.Entries;
        ViewBag.Provider = _config["Database:Provider"];
        ViewBag.Environment = _env.EnvironmentName;
        ViewBag.Versao = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        ViewBag.Uptime = (DateTime.UtcNow - Program.StartedAtUtc).ToString(@"dd\d\ hh\h\ mm\m\ ss\s");
        return View();
    }
}
