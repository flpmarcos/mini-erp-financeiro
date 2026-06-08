using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Regra configurável de aprovação. Define qual alçada (Nível) é exigida conforme
/// faixa de valor e, opcionalmente, categoria / centro de custo / fornecedor.
/// Filtros nulos = "qualquer". Quanto mais filtros preenchidos, mais específica.
/// </summary>
public class RegraAprovacao : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    public bool Ativa { get; set; } = true;

    /// <summary>Faixa de valor a que a regra se aplica (ValorMaximo nulo = sem teto).</summary>
    public decimal ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int? CentroCustoId { get; set; }
    public CentroCusto? CentroCusto { get; set; }

    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    /// <summary>Alçada exigida quando a regra casa.</summary>
    public NivelAprovacao NivelExigido { get; set; } = NivelAprovacao.Gerente;

    /// <summary>Número de filtros preenchidos — usado para escolher a regra mais específica.</summary>
    public int Especificidade =>
        (CategoriaId.HasValue ? 1 : 0) + (CentroCustoId.HasValue ? 1 : 0)
        + (FornecedorId.HasValue ? 1 : 0) + (ValorMaximo.HasValue ? 1 : 0);

    public bool Casa(decimal valor, int categoriaId, int centroCustoId, int fornecedorId) =>
        Ativa
        && valor >= ValorMinimo
        && (!ValorMaximo.HasValue || valor <= ValorMaximo.Value)
        && (!CategoriaId.HasValue || CategoriaId == categoriaId)
        && (!CentroCustoId.HasValue || CentroCustoId == centroCustoId)
        && (!FornecedorId.HasValue || FornecedorId == fornecedorId);
}
