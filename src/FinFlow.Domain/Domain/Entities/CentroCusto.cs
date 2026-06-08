using System.ComponentModel.DataAnnotations;

namespace FinFlow.Domain.Entities;

/// <summary>Centro de custo: onde a despesa e alocada (TI, Comercial, RH...).</summary>
public class CentroCusto : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(60)]
    public string Codigo { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public ICollection<ContaPagar> Contas { get; set; } = new List<ContaPagar>();
}
