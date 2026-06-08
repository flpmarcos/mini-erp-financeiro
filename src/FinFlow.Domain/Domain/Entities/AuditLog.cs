using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Trilha de auditoria: registra quem mudou o que e quando.</summary>
public class AuditLog : BaseEntity, ITenantOwned
{
    /// <summary>Empresa (tenant) — isola a trilha de auditoria por empresa.</summary>
    public int EmpresaId { get; set; } = 1;

    [StringLength(120)]
    public string Usuario { get; set; } = "sistema";

    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    public AcaoAuditoria Acao { get; set; }

    [StringLength(80)]
    public string Entidade { get; set; } = string.Empty;

    public int EntidadeId { get; set; }

    [StringLength(80)]
    public string? Campo { get; set; }

    [StringLength(500)]
    public string? ValorAnterior { get; set; }

    [StringLength(500)]
    public string? ValorNovo { get; set; }

    /// <summary>Endereço IP de origem da ação (capturado automaticamente).</summary>
    [StringLength(50)]
    public string? Ip { get; set; }

    /// <summary>Justificativa para alterações críticas (ex.: mudança de valor/vencimento).</summary>
    [StringLength(500)]
    public string? Motivo { get; set; }
}
