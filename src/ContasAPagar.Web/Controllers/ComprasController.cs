using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContasAPagar.Web.Controllers;

public class ComprasController : BaseController
{
    private readonly ICompraService _compras;
    private readonly IFornecedorService _fornecedores;
    private readonly ICadastroService _cadastros;

    public ComprasController(ICompraService compras, IFornecedorService fornecedores, ICadastroService cadastros)
    {
        _compras = compras;
        _fornecedores = fornecedores;
        _cadastros = cadastros;
    }

    public async Task<IActionResult> Index() => View(await _compras.ListarAsync());

    public async Task<IActionResult> Details(int id)
    {
        var s = await _compras.ObterAsync(id);
        return s is null ? NotFound() : View(s);
    }

    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create()
    {
        await PopularSelectsAsync();
        return View(new CompraFormVM());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create(CompraFormVM vm)
    {
        if (!ModelState.IsValid) { await PopularSelectsAsync(); return View(vm); }
        var r = await _compras.CriarAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularSelectsAsync(); return View(vm); }
        Sucesso("Solicitação de compra criada.");
        return RedirectToAction(nameof(Details), new { id = r.Dados!.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeAprovar)]
    public async Task<IActionResult> Aprovar(int id) => await AcaoAsync(id, _compras.AprovarAsync, "Compra aprovada.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeAprovar)]
    public async Task<IActionResult> Reprovar(int id) => await AcaoAsync(id, _compras.ReprovarAsync, "Compra reprovada.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> EmitirPedido(int id) => await AcaoAsync(id, _compras.EmitirPedidoAsync, "Pedido emitido.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Cancelar(int id) => await AcaoAsync(id, _compras.CancelarAsync, "Compra cancelada.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Receber(int id)
    {
        var r = await _compras.ReceberAsync(id, UsuarioAtual);
        if (r.Sucesso) Sucesso($"Recebido. Conta a pagar #{r.Dados!.Id} gerada.");
        else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> AcaoAsync(int id, Func<int, string, Task<Helpers.OperationResult>> acao, string ok)
    {
        var r = await acao(id, UsuarioAtual);
        if (r.Sucesso) Sucesso(ok); else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopularSelectsAsync()
    {
        ViewBag.Fornecedores = new SelectList(await _fornecedores.ListarAtivosAsync(), "Id", "RazaoSocial");
        ViewBag.Categorias = new SelectList(await _cadastros.ListarCategoriasAsync(), "Id", "Nome");
        ViewBag.Centros = new SelectList(await _cadastros.ListarCentrosAsync(), "Id", "Nome");
    }
}
