using System.ComponentModel.DataAnnotations;

namespace ContasAPagar.Web.Domain.Entities;

/// <summary>Categoria financeira da despesa (Aluguel, Energia, Software...).</summary>
public class Categoria : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public ICollection<ContaPagar> Contas { get; set; } = new List<ContaPagar>();
}
