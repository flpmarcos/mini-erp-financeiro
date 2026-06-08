using System.ComponentModel.DataAnnotations;

namespace FinFlow.Domain.Entities;

/// <summary>Empresa (tenant). Isola fornecedores, contas, cadastros etc. por empresa.</summary>
public class Empresa : BaseEntity
{
    [Required, StringLength(200)]
    public string RazaoSocial { get; set; } = string.Empty;

    [StringLength(14)]
    public string? Cnpj { get; set; }

    public bool Ativa { get; set; } = true;
}

/// <summary>Marca entidades pertencentes a uma empresa (multitenant).</summary>
public interface ITenantOwned
{
    int EmpresaId { get; set; }
}
