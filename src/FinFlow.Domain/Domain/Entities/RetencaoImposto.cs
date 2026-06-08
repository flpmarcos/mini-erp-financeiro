using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Imposto retido na fonte sobre uma conta a pagar (ISS, INSS, IRRF...).</summary>
public class RetencaoImposto : BaseEntity
{
    public int ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public TipoImposto Tipo { get; set; }

    /// <summary>Aliquota em percentual (ex: 5.00 = 5%).</summary>
    public decimal Aliquota { get; set; }

    /// <summary>Valor retido em moeda = base * aliquota / 100.</summary>
    public decimal Valor { get; set; }
}
