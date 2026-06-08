using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Lancamento importado de um extrato bancario (CSV). Base da conciliacao:
/// comparado com as transacoes/baixas do sistema por valor, data e fornecedor.
/// </summary>
public class ExtratoBancarioItem : BaseEntity
{
    public DateTime Data { get; set; }

    [StringLength(250)]
    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    [StringLength(80)]
    public string? Documento { get; set; }

    [StringLength(60)]
    public string? Banco { get; set; }

    [StringLength(40)]
    public string? Tipo { get; set; }

    public StatusConciliacao Status { get; set; } = StatusConciliacao.NaoConciliado;
    public DateTime? DataConciliacao { get; set; }

    /// <summary>Conta a pagar conciliada com este lancamento (quando houver match).</summary>
    public int? ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }
}
