using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Conta a pagar: obrigacao financeira da empresa (boleto, nota, contrato...).
/// E o agregado central do dominio. Regras de negocio ficam nos Services.
/// </summary>
public class ContaPagar : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    // ---- Relacionamentos obrigatorios ----
    [Required]
    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    [Required]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Required]
    public int CentroCustoId { get; set; }
    public CentroCusto? CentroCusto { get; set; }

    // ---- Valores (sempre decimal para dinheiro) ----
    /// <summary>Valor bruto da obrigacao (antes de retencoes de imposto).</summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal ValorOriginal { get; set; }

    /// <summary>Valor liquido = ValorOriginal - impostos retidos. Calculado pelo service.</summary>
    public decimal ValorLiquido { get; set; }

    /// <summary>Valor efetivamente quitado (somatorio das baixas).</summary>
    public decimal ValorPago { get; set; }

    // ---- Datas ----
    public DateTime DataEmissao { get; set; } = DateTime.Today;
    public DateTime DataCompetencia { get; set; } = DateTime.Today;

    [Required]
    public DateTime DataVencimento { get; set; }

    public DateTime? DataPagamento { get; set; }

    // ---- Pagamento ----
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Boleto;

    [StringLength(60)]
    public string? CodigoBarras { get; set; }
    [StringLength(140)]
    public string? ChavePix { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    public StatusConta Status { get; set; } = StatusConta.Rascunho;

    // ---- Parcelamento ----
    /// <summary>Conta origem do parcelamento (a parcela 1..N aponta para a compra-mae). Null = conta avulsa.</summary>
    public int? ContaOrigemId { get; set; }
    public ContaPagar? ContaOrigem { get; set; }
    public ICollection<ContaPagar> Parcelas { get; set; } = new List<ContaPagar>();

    public int NumeroParcela { get; set; } = 1;
    public int TotalParcelas { get; set; } = 1;

    // ---- Colecoes ----
    public ICollection<RetencaoImposto> Retencoes { get; set; } = new List<RetencaoImposto>();
    public ICollection<Aprovacao> Aprovacoes { get; set; } = new List<Aprovacao>();
    public ICollection<BaixaPagamento> Baixas { get; set; } = new List<BaixaPagamento>();
    public ICollection<BankTransaction> Transacoes { get; set; } = new List<BankTransaction>();

    // ---- Helpers de leitura (sem efeito colateral) ----
    public bool EhParcelada => TotalParcelas > 1;
    public decimal SaldoDevedor => ValorLiquido - ValorPago;
    public bool EstaQuitada => SaldoDevedor <= 0m && ValorPago > 0m;
}
