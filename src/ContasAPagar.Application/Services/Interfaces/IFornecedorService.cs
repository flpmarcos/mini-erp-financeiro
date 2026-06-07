using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Helpers;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IFornecedorService
{
    Task<PagedResult<Fornecedor>> ListarAsync(string? busca, int pagina, int tamanho);
    Task<Fornecedor?> ObterAsync(int id);
    Task<List<Fornecedor>> ListarAtivosAsync();
    Task<OperationResult<Fornecedor>> CriarAsync(Fornecedor f);
    Task<OperationResult> AtualizarAsync(Fornecedor f);
}
