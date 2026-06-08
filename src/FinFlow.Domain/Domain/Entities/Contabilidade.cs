using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Conta do plano de contas contábil. Contas "analíticas" recebem lançamento;
/// "sintéticas" só agrupam (ex.: "1 Ativo" agrupa "1.1.01 Caixa").
/// </summary>
public class ContaContabil : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [Required, StringLength(20)]
    public string Codigo { get; set; } = string.Empty;   // ex.: 1.1.01

    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    public TipoContaContabil Tipo { get; set; }
    public NaturezaConta Natureza { get; set; }

    /// <summary>True = aceita lançamento (folha). False = conta de grupo (sintética).</summary>
    public bool Analitica { get; set; } = true;

    public bool Ativa { get; set; } = true;
}

/// <summary>
/// Lançamento contábil (cabeçalho). Segue partida dobrada: a soma dos débitos
/// é igual à soma dos créditos das suas partidas.
/// </summary>
public class LancamentoContabil : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    public DateTime Data { get; set; } = DateTime.Today;

    [Required, StringLength(250)]
    public string Historico { get; set; } = string.Empty;

    /// <summary>Origem: "Manual", "Pagamento:#id", "Recebimento:#id"...</summary>
    [StringLength(60)]
    public string Origem { get; set; } = "Manual";

    public ICollection<PartidaContabil> Partidas { get; set; } = new List<PartidaContabil>();

    public decimal TotalDebito => Partidas.Where(p => p.Tipo == TipoPartida.Debito).Sum(p => p.Valor);
    public decimal TotalCredito => Partidas.Where(p => p.Tipo == TipoPartida.Credito).Sum(p => p.Valor);
    public bool Balanceado => TotalDebito == TotalCredito && TotalDebito > 0m;
}

/// <summary>Partida (linha) do lançamento: débito ou crédito em uma conta contábil.</summary>
public class PartidaContabil : BaseEntity
{
    public int LancamentoContabilId { get; set; }
    public LancamentoContabil? Lancamento { get; set; }

    public int ContaContabilId { get; set; }
    public ContaContabil? Conta { get; set; }

    public TipoPartida Tipo { get; set; }
    public decimal Valor { get; set; }
}
