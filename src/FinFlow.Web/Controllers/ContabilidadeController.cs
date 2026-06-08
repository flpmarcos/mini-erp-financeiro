using FinFlow.Domain.Entities;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinFlow.Controllers;

/// <summary>Contabilidade: plano de contas, lançamentos (partida dobrada), balancete, razão e DRE.</summary>
public class ContabilidadeController : BaseController
{
    private readonly IContabilidadeService _contabil;
    public ContabilidadeController(IContabilidadeService contabil) => _contabil = contabil;

    // ---- Plano de contas ----
    public async Task<IActionResult> PlanoContas() => View(await _contabil.ListarPlanoAsync());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> CriarConta(ContaContabil conta)
    {
        var r = await _contabil.CriarContaAsync(conta);
        if (r.Sucesso) Sucesso("Conta criada."); else Erro(r.Erro!);
        return RedirectToAction(nameof(PlanoContas));
    }

    // ---- Lançamentos ----
    public async Task<IActionResult> Lancamentos() => View(await _contabil.ListarLancamentosAsync());

    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> NovoLancamento()
    {
        await PopularContasAsync();
        var vm = new LancamentoFormVM
        {
            Partidas = new()
            {
                new PartidaInputVM { Tipo = Domain.Enums.TipoPartida.Debito },
                new PartidaInputVM { Tipo = Domain.Enums.TipoPartida.Credito },
            }
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> NovoLancamento(LancamentoFormVM vm)
    {
        var r = await _contabil.CriarLancamentoAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularContasAsync(); return View(vm); }
        Sucesso("Lançamento registrado.");
        return RedirectToAction(nameof(Lancamentos));
    }

    // ---- Relatórios ----
    public async Task<IActionResult> Balancete() => View(await _contabil.BalanceteAsync());

    public async Task<IActionResult> Dre() => View(await _contabil.DreAsync());

    public async Task<IActionResult> Razao(int id)
    {
        var conta = await _contabil.ObterContaAsync(id);
        if (conta is null) return NotFound();
        ViewBag.Conta = conta;
        return View(await _contabil.RazaoAsync(id));
    }

    private async Task PopularContasAsync()
    {
        var analiticas = (await _contabil.ListarPlanoAsync()).Where(c => c.Analitica)
            .Select(c => new { c.Id, Texto = $"{c.Codigo} — {c.Nome}" });
        ViewBag.Contas = new SelectList(analiticas, "Id", "Texto");
    }
}
