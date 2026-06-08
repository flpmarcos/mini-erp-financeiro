using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

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
