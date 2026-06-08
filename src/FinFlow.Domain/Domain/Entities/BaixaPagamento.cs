using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Baixa = registro de um pagamento (total ou parcial) feito sobre uma conta.</summary>
public class BaixaPagamento : BaseEntity
{
    public int ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public DateTime DataPagamento { get; set; } = DateTime.Today;

    /// <summary>Valor pago nesta baixa.</summary>
    public decimal ValorPago { get; set; }

    /// <summary>Multa + juros aplicados quando paga em atraso (informativo).</summary>
    public decimal Encargos { get; set; }

    public int? ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    public FormaPagamento FormaPagamento { get; set; }

    [StringLength(250)]
    public string? Comprovante { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    /// <summary>Obrigatoria quando o valor pago supera o saldo devedor.</summary>
    [StringLength(500)]
    public string? Justificativa { get; set; }
}
