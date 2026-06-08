using FinFlow.Domain.Entities;
using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface ICompraService
{
    Task<List<SolicitacaoCompra>> ListarAsync();
    Task<SolicitacaoCompra?> ObterAsync(int id);
    Task<OperationResult<SolicitacaoCompra>> CriarAsync(CompraFormVM vm, string usuario);
    Task<OperationResult> AprovarAsync(int id, string usuario);
    Task<OperationResult> ReprovarAsync(int id, string usuario);
    Task<OperationResult> EmitirPedidoAsync(int id, string usuario);
    /// <summary>Confirma o recebimento e gera a Conta a Pagar automaticamente.</summary>
    Task<OperationResult<ContaPagar>> ReceberAsync(int id, string usuario);
    Task<OperationResult> CancelarAsync(int id, string usuario);
}
