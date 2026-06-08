using FinFlow.Domain.Enums;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinFlow.Controllers;

public class ContasReceberController : BaseController
{
    private readonly IContaReceberService _contas;
    private readonly IClienteService _clientes;
    private readonly ICadastroService _cadastros;
    private readonly IContabilidadeService _contabil;

    public ContasReceberController(IContaReceberService contas, IClienteService clientes,
        ICadastroService cadastros, IContabilidadeService contabil)
    {
        _contas = contas;
        _clientes = clientes;
        _cadastros = cadastros;
        _contabil = contabil;
    }

    public async Task<IActionResult> Index([FromQuery] ContaReceberFiltroVM filtro)
    {
        if (filtro.Pagina < 1) filtro.Pagina = 1;
        if (filtro.TamanhoPagina < 1) filtro.TamanhoPagina = 10;
        ViewBag.Filtro = filtro;
        ViewBag.Clientes = new SelectList(await _clientes.ListarAtivosAsync(), "Id", "RazaoSocial");
        ViewBag.Status = new SelectList(Enum.GetValues<StatusReceber>().Select(s => new { Id = (int)s, Nome = s.ToString() }), "Id", "Nome");
        return View(await _contas.ListarAsync(filtro));
    }

    public async Task<IActionResult> Details(int id)
    {
        var conta = await _contas.ObterAsync(id);
        return conta is null ? NotFound() : View(conta);
    }

    public async Task<IActionResult> Inadimplencia() => View(await _contas.InadimplentesAsync());

    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create()
    {
        await PopularSelectsAsync();
        return View(new ContaReceberFormVM());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create(ContaReceberFormVM vm)
    {
        if (!ModelState.IsValid) { await PopularSelectsAsync(); return View(vm); }
        var r = await _contas.CriarAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularSelectsAsync(); return View(vm); }
        Sucesso("Conta a receber criada.");
        return RedirectToAction(nameof(Details), new { id = r.Dados!.Id });
    }

    [Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> Receber(int id)
    {
        var conta = await _contas.ObterAsync(id);
        if (conta is null) return NotFound();
        ViewBag.Conta = conta;
        ViewBag.ContasBancarias = new SelectList(await _cadastros.ListarContasBancariasAsync(), "Id", "Nome");
        return View(new RecebimentoVM { ContaReceberId = conta.Id, ValorRecebido = conta.SaldoAReceber });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> Receber(RecebimentoVM vm)
    {
        var r = await _contas.ReceberAsync(vm, UsuarioAtual);
        if (r.Sucesso)
        {
            // Integração contábil: lançamento automático (D Bancos / C Receitas).
            await _contabil.LancarRecebimentoAsync(vm.ContaReceberId, vm.ValorRecebido, UsuarioAtual);
            Sucesso("Recebimento registrado.");
        }
        else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id = vm.ContaReceberId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var r = await _contas.CancelarAsync(id, UsuarioAtual);
        if (r.Sucesso)
        {
            // Reverte o lançamento contábil do recebimento, se houver (no-op se nada foi recebido).
            await _contabil.EstornarRecebimentoAsync(id, UsuarioAtual);
            Sucesso("Conta cancelada.");
        }
        else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopularSelectsAsync()
    {
        ViewBag.Clientes = new SelectList(await _clientes.ListarAtivosAsync(), "Id", "RazaoSocial");
        ViewBag.Categorias = new SelectList(await _cadastros.ListarCategoriasAsync(), "Id", "Nome");
    }
}
