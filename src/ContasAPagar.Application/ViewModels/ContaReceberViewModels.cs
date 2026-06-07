using System.ComponentModel.DataAnnotations;
using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.ViewModels;

public class ContaReceberFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Descricao e obrigatoria"), StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cliente e obrigatorio")]
    [Display(Name = "Cliente")]
    public int ClienteId { get; set; }

    [Display(Name = "Categoria")]
    public int? CategoriaId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    public decimal Valor { get; set; }

    [DataType(DataType.Date)]
    public DateTime DataEmissao { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vencimento e obrigatorio")]
    [DataType(DataType.Date)]
    [Display(Name = "Vencimento")]
    public DateTime DataVencimento { get; set; } = DateTime.Today.AddDays(30);

    [Display(Name = "Forma de Recebimento")]
    public FormaPagamento FormaRecebimento { get; set; } = FormaPagamento.Boleto;

    [StringLength(500)]
    public string? Observacao { get; set; }
}

public class ContaReceberFiltroVM
{
    public StatusReceber? Status { get; set; }
    public int? ClienteId { get; set; }
    [DataType(DataType.Date)] public DateTime? VencimentoDe { get; set; }
    [DataType(DataType.Date)] public DateTime? VencimentoAte { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
}

public class RecebimentoVM
{
    public int ContaReceberId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data do Recebimento")]
    public DateTime DataRecebimento { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero")]
    [Display(Name = "Valor Recebido")]
    public decimal ValorRecebido { get; set; }

    [Display(Name = "Conta Bancária")]
    public int? ContaBancariaId { get; set; }

    [Display(Name = "Forma de Recebimento")]
    public FormaPagamento FormaRecebimento { get; set; } = FormaPagamento.Pix;

    [StringLength(500)]
    public string? Observacao { get; set; }
}
