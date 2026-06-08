using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Conta a receber: fatura/título que um cliente deve à empresa.</summary>
public class ContaReceber : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    public decimal ValorRecebido { get; set; }

    public DateTime DataEmissao { get; set; } = DateTime.Today;
    [Required]
    public DateTime DataVencimento { get; set; }
    public DateTime? DataRecebimento { get; set; }

    public FormaPagamento FormaRecebimento { get; set; } = FormaPagamento.Boleto;

    [StringLength(500)]
    public string? Observacao { get; set; }

    public StatusReceber Status { get; set; } = StatusReceber.Aberta;

    public ICollection<RecebimentoBaixa> Recebimentos { get; set; } = new List<RecebimentoBaixa>();

    public decimal SaldoAReceber => Valor - ValorRecebido;
    public bool EstaInadimplente => Status == StatusReceber.Vencida && SaldoAReceber > 0m;
}

/// <summary>Baixa de recebimento (entrada de dinheiro) sobre uma conta a receber.</summary>
public class RecebimentoBaixa : BaseEntity
{
    public int ContaReceberId { get; set; }
    public ContaReceber? ContaReceber { get; set; }

    public DateTime DataRecebimento { get; set; } = DateTime.Today;
    public decimal ValorRecebido { get; set; }

    public int? ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    public FormaPagamento FormaRecebimento { get; set; }
    [StringLength(500)]
    public string? Observacao { get; set; }
}
