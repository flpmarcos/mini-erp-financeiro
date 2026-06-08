using System.ComponentModel.DataAnnotations;

namespace FinFlow.ViewModels;

public class CompraFormVM
{
    [Required(ErrorMessage = "Descrição é obrigatória"), StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    [Required, Display(Name = "Fornecedor")]
    public int FornecedorId { get; set; }
    [Required, Display(Name = "Categoria")]
    public int CategoriaId { get; set; }
    [Required, Display(Name = "Centro de Custo")]
    public int CentroCustoId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Valor estimado deve ser maior que zero")]
    [Display(Name = "Valor Estimado")]
    public decimal ValorEstimado { get; set; }

    [StringLength(500)]
    public string? Justificativa { get; set; }
}
