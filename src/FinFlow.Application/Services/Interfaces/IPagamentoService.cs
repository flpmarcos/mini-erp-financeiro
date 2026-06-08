using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface IPagamentoService
{
    /// <summary>Baixa (paga) uma conta, dispara a integracao bancaria fake e audita.</summary>
    Task<OperationResult> BaixarAsync(BaixaVM vm, string usuario);
}
