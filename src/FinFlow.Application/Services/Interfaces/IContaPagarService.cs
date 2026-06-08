using FinFlow.Domain.Entities;
using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface IContaPagarService
{
    Task<PagedResult<ContaPagar>> ListarAsync(ContaPagarFiltroVM filtro);
    Task<ContaPagar?> ObterAsync(int id);
    Task<List<ContaPagar>> ListarParcelasAsync(int contaOrigemId);

    Task<OperationResult<ContaPagar>> CriarAsync(ContaPagarFormVM vm, string usuario);
    Task<OperationResult> AtualizarAsync(ContaPagarFormVM vm, string usuario);
    Task<OperationResult> CancelarAsync(int id, string usuario);

    /// <summary>Gera N parcelas vinculadas a uma conta origem.</summary>
    Task<OperationResult<ContaPagar>> GerarParcelamentoAsync(ParcelamentoVM vm, string usuario);

    /// <summary>Reavalia contas pendentes/aprovadas vencidas e marca como Vencida.</summary>
    Task<int> AtualizarVencidasAsync();
}
