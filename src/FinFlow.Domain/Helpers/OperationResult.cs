namespace FinFlow.Helpers;

/// <summary>
/// Resultado de uma operacao de negocio. Evita lancar excecao para erro de regra:
/// o Service retorna Falha("mensagem amigavel") e a Controller decide o que mostrar.
/// </summary>
public class OperationResult
{
    public bool Sucesso { get; protected set; }
    public string? Erro { get; protected set; }

    public static OperationResult Ok() => new() { Sucesso = true };
    public static OperationResult Falha(string erro) => new() { Sucesso = false, Erro = erro };
}

/// <summary>Resultado com payload de dados.</summary>
public class OperationResult<T> : OperationResult
{
    public T? Dados { get; private set; }

    public static OperationResult<T> Ok(T dados) => new() { Sucesso = true, Dados = dados };
    public static new OperationResult<T> Falha(string erro) => new() { Sucesso = false, Erro = erro };
}
