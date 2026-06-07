using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.ViewModels;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IClienteService
{
    Task<PagedResult<Cliente>> ListarAsync(string? busca, int pagina, int tamanho);
    Task<List<Cliente>> ListarAtivosAsync();
    Task<Cliente?> ObterAsync(int id);
    Task<OperationResult<Cliente>> CriarAsync(Cliente c);
    Task<OperationResult> AtualizarAsync(Cliente c);
}

public interface IContaReceberService
{
    Task<PagedResult<ContaReceber>> ListarAsync(ContaReceberFiltroVM filtro);
    Task<ContaReceber?> ObterAsync(int id);
    Task<OperationResult<ContaReceber>> CriarAsync(ContaReceberFormVM vm, string usuario);
    Task<OperationResult> ReceberAsync(RecebimentoVM vm, string usuario);
    Task<OperationResult> CancelarAsync(int id, string usuario);
    Task<int> AtualizarVencidasAsync();
    Task<List<ContaReceber>> InadimplentesAsync();
}
