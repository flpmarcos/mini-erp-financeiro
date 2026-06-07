using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Controllers;

public class RelatoriosController : BaseController
{
    private readonly IRelatorioService _rel;
    public RelatoriosController(IRelatorioService rel) => _rel = rel;

    public IActionResult Index() => View();

    // ---- Relatorios baseados em lista de contas (view compartilhada ListaContas) ----
    public async Task<IActionResult> Vencidas() => Lista("Contas Vencidas", await _rel.ContasVencidasAsync());
    public async Task<IActionResult> AVencer() => Lista("A Vencer (proximos 7 dias)", await _rel.AVencer7DiasAsync());
    public async Task<IActionResult> PagasNoMes() => Lista("Pagas no Mes", await _rel.PagasNoMesAsync());
    public async Task<IActionResult> Pendentes() => Lista("Contas Pendentes", await _rel.PendentesAsync());
    public async Task<IActionResult> ParcialmentePagas() => Lista("Parcialmente Pagas", await _rel.ParcialmentePagasAsync());

    // ---- Relatorios agregados (view compartilhada Grafico) ----
    public async Task<IActionResult> FluxoCaixa() => Grafico("Fluxo de Caixa Previsto", await _rel.FluxoCaixaPrevistoAsync());
    public async Task<IActionResult> PorFornecedor() => Grafico("Despesas por Fornecedor", await _rel.DespesasPorFornecedorAsync());
    public async Task<IActionResult> PorCentroCusto() => Grafico("Despesas por Centro de Custo", await _rel.DespesasPorCentroCustoAsync());
    public async Task<IActionResult> PorCategoria() => Grafico("Despesas por Categoria", await _rel.DespesasPorCategoriaAsync());
    public async Task<IActionResult> Impostos() => Grafico("Impostos Retidos", await _rel.ImpostosRetidosAsync());
    public async Task<IActionResult> PorBanco() => Grafico("Pagamentos por Banco", await _rel.PagamentosPorBancoAsync());

    private IActionResult Lista(string titulo, object dados)
    {
        ViewBag.Titulo = titulo;
        return View("ListaContas", dados);
    }

    private IActionResult Grafico(string titulo, object dados)
    {
        ViewBag.Titulo = titulo;
        return View("Grafico", dados);
    }
}
