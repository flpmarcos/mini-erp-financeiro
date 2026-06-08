using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Evento de webhook recebido do banco (confirmação assíncrona de pagamento).
/// Persistido para auditoria/idempotência. Processado atualiza a BankTransaction.
/// </summary>
public class BankWebhookEvent : BaseEntity
{
    [Required, StringLength(80)]
    public string CodigoTransacao { get; set; } = string.Empty;

    public StatusTransacaoBancaria StatusInformado { get; set; }

    public string? Payload { get; set; }

    public DateTime RecebidoEm { get; set; } = DateTime.UtcNow;
    public bool Processado { get; set; }
    [StringLength(300)]
    public string? Resultado { get; set; }
}
