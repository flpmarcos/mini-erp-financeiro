using FinFlow.Domain.Entities;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

public record ResultadoImportacao(int Importados, int ConciliadosAutomaticamente);

public interface IConciliacaoService
{
    /// <summary>Importa um extrato CSV e tenta conciliar automaticamente por valor/data.</summary>
    Task<OperationResult<ResultadoImportacao>> ImportarCsvAsync(Stream csv, string usuario);

    Task<List<ExtratoBancarioItem>> ListarAsync();

    /// <summary>Concilia manualmente um lancamento do extrato com uma conta A PAGAR.</summary>
    Task<OperationResult> ConciliarManualAsync(int extratoItemId, int contaPagarId, string usuario);

    /// <summary>Concilia manualmente um lancamento do extrato com uma conta A RECEBER.</summary>
    Task<OperationResult> ConciliarReceberManualAsync(int extratoItemId, int contaReceberId, string usuario);
}
