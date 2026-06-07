using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class AnexosController : BaseController
{
    private readonly IAnexoService _anexos;
    public AnexosController(IAnexoService anexos) => _anexos = anexos;

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Upload(int contaPagarId, TipoAnexo tipo, IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            Erro("Selecione um arquivo.");
            return RedirectToAction("Details", "Contas", new { id = contaPagarId });
        }

        await using var stream = arquivo.OpenReadStream();
        var r = await _anexos.UploadAsync(contaPagarId, arquivo.FileName, arquivo.ContentType,
            arquivo.Length, stream, tipo, UsuarioAtual);

        if (r.Sucesso) Sucesso("Anexo enviado."); else Erro(r.Erro!);
        return RedirectToAction("Details", "Contas", new { id = contaPagarId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var arq = await _anexos.BaixarAsync(id);
        return arq is null ? NotFound() : File(arq.Conteudo, arq.ContentType, arq.Nome);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Delete(int id, int contaPagarId)
    {
        var r = await _anexos.RemoverAsync(id, UsuarioAtual);
        if (r.Sucesso) Sucesso("Anexo removido."); else Erro(r.Erro!);
        return RedirectToAction("Details", "Contas", new { id = contaPagarId });
    }
}
