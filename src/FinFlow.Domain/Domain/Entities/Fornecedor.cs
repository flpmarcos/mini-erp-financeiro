using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Fornecedor / credor. Quem recebe os pagamentos.</summary>
public class Fornecedor : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required(ErrorMessage = "Razao social e obrigatoria")]
    [StringLength(200)]
    public string RazaoSocial { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NomeFantasia { get; set; }

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cnpj;

    /// <summary>CNPJ (14) ou CPF (11), somente digitos.</summary>
    [Required(ErrorMessage = "Documento e obrigatorio")]
    [StringLength(14)]
    public string Documento { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Telefone { get; set; }

    [StringLength(250)]
    public string? Endereco { get; set; }

    // Dados bancarios para pagamento
    [StringLength(60)]
    public string? Banco { get; set; }
    [StringLength(10)]
    public string? Agencia { get; set; }
    [StringLength(20)]
    public string? Conta { get; set; }
    public TipoContaBancaria? TipoConta { get; set; }
    [StringLength(140)]
    public string? ChavePix { get; set; }

    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;

    public ICollection<ContaPagar> Contas { get; set; } = new List<ContaPagar>();

    public bool PodeReceberPagamento => Status == StatusFornecedor.Ativo;
}
