using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Registro de uma decisao de aprovacao/reprovacao sobre uma conta.</summary>
public class Aprovacao : BaseEntity
{
    public int ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public NivelAprovacao NivelExigido { get; set; }
    public ResultadoAprovacao Resultado { get; set; } = ResultadoAprovacao.Pendente;

    [StringLength(120)]
    public string? Aprovador { get; set; }
    public DateTime? DataDecisao { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}
