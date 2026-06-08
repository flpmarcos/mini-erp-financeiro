using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Controllers;

public class ConciliacaoController : BaseController
{
    private readonly IConciliacaoService _conciliacao;
    public ConciliacaoController(IConciliacaoService conciliacao) => _conciliacao = conciliacao;

    public async Task<IActionResult> Index()
    {
        var itens = await _conciliacao.ListarAsync();
        return View(itens);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Importar(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            Erro("Selecione um arquivo CSV.");
            return RedirectToAction(nameof(Index));
        }

        await using var stream = arquivo.OpenReadStream();
        var r = await _conciliacao.ImportarCsvAsync(stream, UsuarioAtual);
        if (r.Sucesso)
            Sucesso($"Importados {r.Dados!.Importados} lancamentos, {r.Dados.ConciliadosAutomaticamente} conciliados automaticamente.");
        else
            Erro(r.Erro!);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> ConciliarManual(int extratoItemId, int contaPagarId)
    {
        var r = await _conciliacao.ConciliarManualAsync(extratoItemId, contaPagarId, UsuarioAtual);
        if (r.Sucesso) Sucesso("Lançamento conciliado com conta a pagar."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> ConciliarReceber(int extratoItemId, int contaReceberId)
    {
        var r = await _conciliacao.ConciliarReceberManualAsync(extratoItemId, contaReceberId, UsuarioAtual);
        if (r.Sucesso) Sucesso("Lançamento conciliado com conta a receber."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Index));
    }
}
