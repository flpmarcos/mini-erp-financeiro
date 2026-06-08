using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Solicitação de compra (Fase 8). Fluxo: Solicitada → Aprovada → PedidoEmitido →
/// Recebida (gera Conta a Pagar automaticamente). Conta só nasce após recebimento.
/// </summary>
public class SolicitacaoCompra : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    [Required]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Required]
    public int CentroCustoId { get; set; }
    public CentroCusto? CentroCusto { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor estimado deve ser maior que zero")]
    public decimal ValorEstimado { get; set; }

    [StringLength(500)]
    public string? Justificativa { get; set; }

    public StatusCompra Status { get; set; } = StatusCompra.Solicitada;

    [StringLength(120)]
    public string SolicitadoPor { get; set; } = "sistema";

    public DateTime? DataRecebimento { get; set; }

    /// <summary>Conta a pagar gerada no recebimento (rastreabilidade).</summary>
    public int? ContaPagarGeradaId { get; set; }
    public ContaPagar? ContaPagarGerada { get; set; }
}
