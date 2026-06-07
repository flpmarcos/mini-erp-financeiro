using ContasAPagar.Web.ViewModels;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IFluxoCaixaService
{
    /// <summary>Monta o fluxo de caixa: saldo atual + projeções nos horizontes informados.</summary>
    Task<FluxoCaixaVM> ObterAsync(params int[] horizontesDias);
}
