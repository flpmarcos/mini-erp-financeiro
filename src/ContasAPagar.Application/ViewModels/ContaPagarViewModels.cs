using System.ComponentModel.DataAnnotations;
using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.ViewModels;

/// <summary>Uma retencao de imposto informada no formulario.</summary>
public class RetencaoInputVM
{
    public TipoImposto Tipo { get; set; }
    [Range(0, 100)]
    public decimal Aliquota { get; set; }
}

/// <summary>Formulario de criacao/edicao de conta a pagar.</summary>
public class ContaPagarFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Descricao e obrigatoria"), StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fornecedor e obrigatorio")]
    [Display(Name = "Fornecedor")]
    public int FornecedorId { get; set; }

    [Required(ErrorMessage = "Categoria e obrigatoria")]
    [Display(Name = "Categoria")]
    public int CategoriaId { get; set; }

    [Required(ErrorMessage = "Centro de custo e obrigatorio")]
    [Display(Name = "Centro de Custo")]
    public int CentroCustoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    [Display(Name = "Valor Original")]
    public decimal ValorOriginal { get; set; }

    [Display(Name = "Emissao")]
    [DataType(DataType.Date)]
    public DateTime DataEmissao { get; set; } = DateTime.Today;

    [Display(Name = "Competencia")]
    [DataType(DataType.Date)]
    public DateTime DataCompetencia { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vencimento e obrigatorio")]
    [Display(Name = "Vencimento")]
    [DataType(DataType.Date)]
    public DateTime DataVencimento { get; set; } = DateTime.Today.AddDays(30);

    [Display(Name = "Forma de Pagamento")]
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Boleto;

    [Display(Name = "Codigo de Barras")]
    [StringLength(60)]
    public string? CodigoBarras { get; set; }

    [Display(Name = "Chave PIX")]
    [StringLength(140)]
    public string? ChavePix { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    public List<RetencaoInputVM> Retencoes { get; set; } = new();
}

/// <summary>Filtros da listagem de contas a pagar.</summary>
public class ContaPagarFiltroVM
{
    public StatusConta? Status { get; set; }
    public int? FornecedorId { get; set; }
    public int? CentroCustoId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? VencimentoDe { get; set; }
    [DataType(DataType.Date)]
    public DateTime? VencimentoAte { get; set; }

    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

/// <summary>Geracao de compra parcelada.</summary>
public class ParcelamentoVM
{
    [Required, StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required, Display(Name = "Fornecedor")]
    public int FornecedorId { get; set; }
    [Required, Display(Name = "Categoria")]
    public int CategoriaId { get; set; }
    [Required, Display(Name = "Centro de Custo")]
    public int CentroCustoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor total deve ser maior que zero")]
    [Display(Name = "Valor Total")]
    public decimal ValorTotal { get; set; }

    [Range(2, 360, ErrorMessage = "Numero de parcelas deve ser entre 2 e 360")]
    [Display(Name = "Quantidade de Parcelas")]
    public int Parcelas { get; set; } = 2;

    [Display(Name = "Vencimento da 1a Parcela")]
    [DataType(DataType.Date)]
    public DateTime PrimeiroVencimento { get; set; } = DateTime.Today.AddDays(30);

    [Display(Name = "Forma de Pagamento")]
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Boleto;
}

/// <summary>Baixa (pagamento) de uma conta.</summary>
public class BaixaVM
{
    public int ContaPagarId { get; set; }

    [Required]
    [Display(Name = "Data do Pagamento")]
    [DataType(DataType.Date)]
    public DateTime DataPagamento { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor pago deve ser maior que zero")]
    [Display(Name = "Valor Pago")]
    public decimal ValorPago { get; set; }

    [Required(ErrorMessage = "Selecione a conta bancaria")]
    [Display(Name = "Conta Bancaria")]
    public int ContaBancariaId { get; set; }

    [Display(Name = "Forma de Pagamento")]
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Pix;

    [StringLength(250)]
    public string? Comprovante { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    [Display(Name = "Justificativa (se valor maior que o devido)")]
    [StringLength(500)]
    public string? Justificativa { get; set; }
}

/// <summary>Decisao de aprovacao/reprovacao.</summary>
public class DecisaoAprovacaoVM
{
    public int ContaPagarId { get; set; }
    [Required(ErrorMessage = "Informe o aprovador")]
    public string Aprovador { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Observacao { get; set; }
}
