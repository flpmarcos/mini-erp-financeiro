using FinFlow.Domain.Enums;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinFlow.Controllers;

public class ContasController : BaseController
{
    private readonly IContaPagarService _contas;
    private readonly IPagamentoService _pagamento;
    private readonly IAprovacaoService _aprovacao;
    private readonly IJurosMultaService _juros;
    private readonly IFornecedorService _fornecedores;
    private readonly ICadastroService _cadastros;
    private readonly IAnexoService _anexos;
    private readonly IBankIntegrationService _bank;
    private readonly IContabilidadeService _contabil;

    public ContasController(IContaPagarService contas, IPagamentoService pagamento,
        IAprovacaoService aprovacao, IJurosMultaService juros,
        IFornecedorService fornecedores, ICadastroService cadastros, IAnexoService anexos,
        IBankIntegrationService bank, IContabilidadeService contabil)
    {
        _contas = contas;
        _pagamento = pagamento;
        _aprovacao = aprovacao;
        _juros = juros;
        _fornecedores = fornecedores;
        _cadastros = cadastros;
        _anexos = anexos;
        _bank = bank;
        _contabil = contabil;
    }

    // ---- Listagem com filtros + paginacao ----
    public async Task<IActionResult> Index([FromQuery] ContaPagarFiltroVM filtro)
    {
        if (filtro.Pagina < 1) filtro.Pagina = 1;
        if (filtro.TamanhoPagina < 1) filtro.TamanhoPagina = 10;

        await PopularFiltrosAsync();
        ViewBag.Filtro = filtro;
        var resultado = await _contas.ListarAsync(filtro);
        return View(resultado);
    }

    // ---- Detalhes (mostra encargos calculados em tempo real) ----
    public async Task<IActionResult> Details(int id)
    {
        var conta = await _contas.ObterAsync(id);
        if (conta is null) return NotFound();

        ViewBag.Encargos = _juros.Calcular(conta);
        if (conta.ContaOrigemId.HasValue || conta.NumeroParcela > 0)
            ViewBag.Parcelas = await _contas.ListarParcelasAsync(conta.ContaOrigemId ?? conta.Id);
        ViewBag.Anexos = await _anexos.ListarAsync(conta.Id);

        return View(conta);
    }

    // ---- Criar ----
    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create()
    {
        await PopularSelectsAsync();
        return View(new ContaPagarFormVM { Retencoes = LinhasImpostoPadrao() });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Create(ContaPagarFormVM vm)
    {
        if (!ModelState.IsValid) { await PopularSelectsAsync(); return View(vm); }

        var r = await _contas.CriarAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularSelectsAsync(); return View(vm); }

        Sucesso("Conta criada.");
        return RedirectToAction(nameof(Details), new { id = r.Dados!.Id });
    }

    // ---- Editar ----
    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(int id)
    {
        var conta = await _contas.ObterAsync(id);
        if (conta is null) return NotFound();

        var vm = new ContaPagarFormVM
        {
            Id = conta.Id,
            Descricao = conta.Descricao,
            FornecedorId = conta.FornecedorId,
            CategoriaId = conta.CategoriaId,
            CentroCustoId = conta.CentroCustoId,
            ValorOriginal = conta.ValorOriginal,
            DataEmissao = conta.DataEmissao,
            DataCompetencia = conta.DataCompetencia,
            DataVencimento = conta.DataVencimento,
            FormaPagamento = conta.FormaPagamento,
            CodigoBarras = conta.CodigoBarras,
            ChavePix = conta.ChavePix,
            Observacao = conta.Observacao,
            Retencoes = LinhasImpostoPadrao(conta.Retencoes.ToDictionary(x => x.Tipo, x => x.Aliquota))
        };

        await PopularSelectsAsync();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Edit(ContaPagarFormVM vm)
    {
        if (!ModelState.IsValid) { await PopularSelectsAsync(); return View(vm); }

        var r = await _contas.AtualizarAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularSelectsAsync(); return View(vm); }

        Sucesso("Conta atualizada.");
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var r = await _contas.CancelarAsync(id, UsuarioAtual);
        if (r.Sucesso) Sucesso("Conta cancelada."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> EnviarAprovacao(int id)
    {
        var r = await _aprovacao.EnviarParaAprovacaoAsync(id, UsuarioAtual);
        if (r.Sucesso) Sucesso("Conta enviada para aprovacao."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Parcelamento ----
    [Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Parcelar()
    {
        await PopularSelectsAsync();
        return View(new ParcelamentoVM());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodeCadastrar)]
    public async Task<IActionResult> Parcelar(ParcelamentoVM vm)
    {
        if (!ModelState.IsValid) { await PopularSelectsAsync(); return View(vm); }

        var r = await _contas.GerarParcelamentoAsync(vm, UsuarioAtual);
        if (!r.Sucesso) { Erro(r.Erro!); await PopularSelectsAsync(); return View(vm); }

        Sucesso($"Parcelamento gerado: {vm.Parcelas} parcelas.");
        return RedirectToAction(nameof(Index));
    }

    // ---- Baixa / pagamento ----
    [Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> Baixar(int id)
    {
        var conta = await _contas.ObterAsync(id);
        if (conta is null) return NotFound();

        var encargos = _juros.Calcular(conta);
        ViewBag.Conta = conta;
        ViewBag.Encargos = encargos;
        ViewBag.ContasBancarias = new SelectList(await _cadastros.ListarContasBancariasAsync(), "Id", "Nome");

        return View(new BaixaVM
        {
            ContaPagarId = conta.Id,
            ValorPago = encargos.ValorAtualizado,
            FormaPagamento = conta.FormaPagamento
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> Estornar(int id, string motivo)
    {
        var r = await _bank.EstornarAsync(id, motivo, UsuarioAtual);
        if (r.Sucesso) Sucesso("Pagamento estornado."); else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.PodePagar)]
    public async Task<IActionResult> Baixar(BaixaVM vm)
    {
        var r = await _pagamento.BaixarAsync(vm, UsuarioAtual);
        if (r.Sucesso)
        {
            // Integração contábil: gera o lançamento automático (D Despesas / C Bancos).
            await _contabil.LancarPagamentoAsync(vm.ContaPagarId, vm.ValorPago, UsuarioAtual);
            Sucesso("Pagamento registrado com sucesso.");
        }
        else Erro(r.Erro!);
        return RedirectToAction(nameof(Details), new { id = vm.ContaPagarId });
    }

    // ---- helpers de UI ----
    private async Task PopularSelectsAsync()
    {
        ViewBag.Fornecedores = new SelectList(await _fornecedores.ListarAtivosAsync(), "Id", "RazaoSocial");
        ViewBag.Categorias = new SelectList(await _cadastros.ListarCategoriasAsync(), "Id", "Nome");
        ViewBag.Centros = new SelectList(await _cadastros.ListarCentrosAsync(), "Id", "Nome");
    }

    private async Task PopularFiltrosAsync()
    {
        ViewBag.Fornecedores = new SelectList(await _fornecedores.ListarAtivosAsync(), "Id", "RazaoSocial");
        ViewBag.Centros = new SelectList(await _cadastros.ListarCentrosAsync(), "Id", "Nome");
        ViewBag.Status = new SelectList(Enum.GetValues<StatusConta>().Select(s => new { Id = (int)s, Nome = s.ToString() }), "Id", "Nome");
    }

    /// <summary>Gera as 6 linhas de imposto para o formulario (aliquotas preenchidas opcionalmente).</summary>
    private static List<RetencaoInputVM> LinhasImpostoPadrao(Dictionary<TipoImposto, decimal>? existentes = null) =>
        Enum.GetValues<TipoImposto>()
            .Select(t => new RetencaoInputVM { Tipo = t, Aliquota = existentes != null && existentes.TryGetValue(t, out var a) ? a : 0m })
            .ToList();
}
