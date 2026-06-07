namespace ContasAPagar.Web.Api;

/// <summary>Envelope padrao de resposta da API (sucesso/erro + dados + meta).</summary>
public record ApiResponse<T>(bool Success, T? Data = default, string? Error = null, object? Meta = null);

public record PageMeta(int Total, int Page, int PageSize, int TotalPages);

public record ContaPagarDto(
    int Id, string Descricao, string? Fornecedor, decimal ValorLiquido,
    decimal ValorPago, decimal SaldoDevedor, DateTime Vencimento, string Status);

public record FornecedorDto(
    int Id, string RazaoSocial, string? NomeFantasia, string Documento, string Status);

public record ContaReceberDto(
    int Id, string Descricao, string? Cliente, decimal Valor,
    decimal ValorRecebido, decimal SaldoAReceber, DateTime Vencimento, string Status);
