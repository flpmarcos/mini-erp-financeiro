using System.ComponentModel.DataAnnotations;
using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.Domain.Entities;

/// <summary>Cliente / sacado. Quem deve para a empresa (Contas a Receber).</summary>
public class Cliente : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required(ErrorMessage = "Nome/Razao social e obrigatorio"), StringLength(200)]
    public string RazaoSocial { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NomeFantasia { get; set; }

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cnpj;

    [Required, StringLength(14)]
    public string Documento { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }
    [StringLength(20)]
    public string? Telefone { get; set; }
    [StringLength(250)]
    public string? Endereco { get; set; }

    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;

    public ICollection<ContaReceber> Contas { get; set; } = new List<ContaReceber>();
}
