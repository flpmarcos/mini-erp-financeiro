using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Helpers;

namespace ContasAPagar.Web.Services.Interfaces;

public record ResultadoImportacao(int Importados, int ConciliadosAutomaticamente);

public interface IConciliacaoService
{
    /// <summary>Importa um extrato CSV e tenta conciliar automaticamente por valor/data.</summary>
    Task<OperationResult<ResultadoImportacao>> ImportarCsvAsync(Stream csv, string usuario);

    Task<List<ExtratoBancarioItem>> ListarAsync();

    /// <summary>Concilia manualmente um lancamento do extrato com uma conta paga.</summary>
    Task<OperationResult> ConciliarManualAsync(int extratoItemId, int contaPagarId, string usuario);
}
