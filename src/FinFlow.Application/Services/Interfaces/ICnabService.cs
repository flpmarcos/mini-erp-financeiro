using FinFlow.Integrations.Cnab;

namespace FinFlow.Services.Interfaces;

public interface ICnabService
{
    /// <summary>Gera o conteúdo do arquivo de remessa (contas liberadas/pendentes a pagar).</summary>
    Task<string> GerarRemessaAsync();

    /// <summary>Processa um arquivo de retorno: confirma/recusa pagamentos. Linhas inválidas são ignoradas.</summary>
    Task<ResultadoRetornoCnab> ProcessarRetornoAsync(Stream arquivo, string usuario);
}
