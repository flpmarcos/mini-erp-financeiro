using FinFlow.Domain.Entities;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

public interface IFornecedorService
{
    Task<PagedResult<Fornecedor>> ListarAsync(string? busca, int pagina, int tamanho);
    Task<Fornecedor?> ObterAsync(int id);
    Task<List<Fornecedor>> ListarAtivosAsync();
    Task<OperationResult<Fornecedor>> CriarAsync(Fornecedor f);
    Task<OperationResult> AtualizarAsync(Fornecedor f);
}
