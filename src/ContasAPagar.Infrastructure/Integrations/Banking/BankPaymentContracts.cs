using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.Integrations.Banking;

/// <summary>Requisicao de pagamento enviada a um banco (contrato neutro de banco).</summary>
public record BankPaymentRequest
{
    public int ContaPagarId { get; init; }
    public decimal Valor { get; init; }
    public FormaPagamento Forma { get; init; }
    public string Favorecido { get; init; } = string.Empty;
    public string? ChavePix { get; init; }
    public string? CodigoBarras { get; init; }
    public string? Documento { get; init; }
}

/// <summary>Resposta padronizada do banco apos tentativa de pagamento.</summary>
public record BankPaymentResponse
{
    public StatusTransacaoBancaria Status { get; init; }
    public string? CodigoTransacao { get; init; }
    public string? MensagemErro { get; init; }
    public string PayloadEnvio { get; init; } = string.Empty;
    public string PayloadResposta { get; init; } = string.Empty;
}

/// <summary>
/// Porta de saida para integracao bancaria. Hoje implementada por fakes.
/// No futuro, basta criar um adapter real (ex: ItauBankAdapter) implementando esta interface.
/// </summary>
public interface IBankPaymentService
{
    BancoIntegracao Banco { get; }
    Task<BankPaymentResponse> PagarAsync(BankPaymentRequest request);
}
