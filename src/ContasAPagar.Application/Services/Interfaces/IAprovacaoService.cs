using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.ViewModels;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IAprovacaoService
{
    /// <summary>Define a alcada exigida conforme o valor liquido.</summary>
    NivelAprovacao DeterminarNivel(decimal valor);

    /// <summary>Envia a conta para o fluxo de aprovacao (auto-aprova abaixo do limite).</summary>
    Task<OperationResult> EnviarParaAprovacaoAsync(int contaId, string usuario);

    Task<OperationResult> AprovarAsync(DecisaoAprovacaoVM vm);
    Task<OperationResult> ReprovarAsync(DecisaoAprovacaoVM vm);

    Task<List<ContaPagar>> ListarPendentesAsync();
}
