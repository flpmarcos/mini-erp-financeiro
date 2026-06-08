using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Api;

/// <summary>
/// API REST auxiliar (somente leitura nesta fase). Mesmo projeto MVC, rotas /api/v1/*.
/// Protegida por cookie (logue na app) — para JWT veja README. Documentada via Swagger (/swagger).
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v1/accounts-payable")]
public class AccountsPayableApiController : ControllerBase
{
    private readonly IContaPagarService _contas;
    public AccountsPayableApiController(IContaPagarService contas) => _contas = contas;

    /// <summary>Lista contas a pagar paginadas (filtros via querystring).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContaPagarDto>>>> Get([FromQuery] ContaPagarFiltroVM filtro)
    {
        if (filtro.Pagina < 1) filtro.Pagina = 1;
        if (filtro.TamanhoPagina is < 1 or > 100) filtro.TamanhoPagina = 20;

        var page = await _contas.ListarAsync(filtro);
        var dtos = page.Itens.Select(c => new ContaPagarDto(
            c.Id, c.Descricao, c.Fornecedor?.RazaoSocial, c.ValorLiquido, c.ValorPago,
            c.SaldoDevedor, c.DataVencimento, c.Status.ToString()));

        var meta = new PageMeta(page.TotalItens, page.Pagina, page.TamanhoPagina, page.TotalPaginas);
        return Ok(new ApiResponse<IEnumerable<ContaPagarDto>>(true, dtos, Meta: meta));
    }

    /// <summary>Obtém uma conta a pagar por id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ContaPagarDto>>> GetById(int id)
    {
        var c = await _contas.ObterAsync(id);
        if (c is null) return NotFound(new ApiResponse<ContaPagarDto>(false, Error: "Conta nao encontrada."));
        var dto = new ContaPagarDto(c.Id, c.Descricao, c.Fornecedor?.RazaoSocial, c.ValorLiquido,
            c.ValorPago, c.SaldoDevedor, c.DataVencimento, c.Status.ToString());
        return Ok(new ApiResponse<ContaPagarDto>(true, dto));
    }
}

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v1/suppliers")]
public class SuppliersApiController : ControllerBase
{
    private readonly IFornecedorService _fornecedores;
    public SuppliersApiController(IFornecedorService fornecedores) => _fornecedores = fornecedores;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<FornecedorDto>>>> Get([FromQuery] string? busca, int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        var result = await _fornecedores.ListarAsync(busca, page, pageSize);
        var dtos = result.Itens.Select(f => new FornecedorDto(f.Id, f.RazaoSocial, f.NomeFantasia, f.Documento, f.Status.ToString()));
        var meta = new PageMeta(result.TotalItens, result.Pagina, result.TamanhoPagina, result.TotalPaginas);
        return Ok(new ApiResponse<IEnumerable<FornecedorDto>>(true, dtos, Meta: meta));
    }
}

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v1/accounts-receivable")]
public class AccountsReceivableApiController : ControllerBase
{
    private readonly IContaReceberService _contas;
    public AccountsReceivableApiController(IContaReceberService contas) => _contas = contas;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContaReceberDto>>>> Get([FromQuery] ContaReceberFiltroVM filtro)
    {
        if (filtro.Pagina < 1) filtro.Pagina = 1;
        if (filtro.TamanhoPagina is < 1 or > 100) filtro.TamanhoPagina = 20;
        var page = await _contas.ListarAsync(filtro);
        var dtos = page.Itens.Select(c => new ContaReceberDto(
            c.Id, c.Descricao, c.Cliente?.RazaoSocial, c.Valor, c.ValorRecebido,
            c.SaldoAReceber, c.DataVencimento, c.Status.ToString()));
        var meta = new PageMeta(page.TotalItens, page.Pagina, page.TamanhoPagina, page.TotalPaginas);
        return Ok(new ApiResponse<IEnumerable<ContaReceberDto>>(true, dtos, Meta: meta));
    }
}

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/v1/cash-flow")]
public class CashFlowApiController : ControllerBase
{
    private readonly IFluxoCaixaService _fluxo;
    public CashFlowApiController(IFluxoCaixaService fluxo) => _fluxo = fluxo;

    /// <summary>Fluxo de caixa consolidado (saldo atual + projeções 7/30/90 dias).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<FluxoCaixaVM>>> Get()
        => Ok(new ApiResponse<FluxoCaixaVM>(true, await _fluxo.ObterAsync(7, 30, 90)));
}
