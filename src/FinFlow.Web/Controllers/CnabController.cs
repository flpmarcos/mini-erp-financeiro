using System.Text;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Controllers;

/// <summary>Geração de remessa e processamento de retorno CNAB (fake/didático).</summary>
public class CnabController : BaseController
{
    private readonly ICnabService _cnab;
    public CnabController(ICnabService cnab) => _cnab = cnab;

    public IActionResult Index() => View();

    [HttpGet, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> GerarRemessa()
    {
        var conteudo = await _cnab.GerarRemessaAsync();
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        var nome = $"remessa-{DateTime.Today:yyyyMMdd}.rem";
        return File(bytes, "text/plain", nome);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> ProcessarRetorno(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            Erro("Selecione o arquivo de retorno.");
            return RedirectToAction(nameof(Index));
        }

        await using var stream = arquivo.OpenReadStream();
        var r = await _cnab.ProcessarRetornoAsync(stream, UsuarioAtual);
        Sucesso($"Retorno processado: {r.Confirmados} confirmados, {r.Rejeitados} rejeitados, {r.Ignorados} ignorados.");
        return RedirectToAction(nameof(Index));
    }
}
