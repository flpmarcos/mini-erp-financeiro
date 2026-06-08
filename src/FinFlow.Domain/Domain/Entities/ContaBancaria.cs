using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Conta bancaria DA EMPRESA, de onde sai o dinheiro nos pagamentos.</summary>
public class ContaBancaria : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Banco { get; set; } = string.Empty;

    public BancoIntegracao BancoIntegracao { get; set; } = BancoIntegracao.Generico;

    [StringLength(10)]
    public string? Agencia { get; set; }
    [StringLength(20)]
    public string? Conta { get; set; }
    public TipoContaBancaria TipoConta { get; set; } = TipoContaBancaria.CorrentePessoaJuridica;

    [DataType(DataType.Currency)]
    public decimal SaldoInicial { get; set; }

    public bool Ativo { get; set; } = true;
}
