using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.ViewModels;

public class PartidaInputVM
{
    public int ContaContabilId { get; set; }
    public TipoPartida Tipo { get; set; }
    public decimal Valor { get; set; }
}

public class LancamentoFormVM
{
    [DataType(DataType.Date)]
    public DateTime Data { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Histórico é obrigatório"), StringLength(250)]
    public string Historico { get; set; } = string.Empty;

    public List<PartidaInputVM> Partidas { get; set; } = new();
}

/// <summary>Linha do balancete (saldo por conta).</summary>
public record BalanceteLinha(string Codigo, string Nome, string Tipo, decimal Debito, decimal Credito, decimal Saldo);

/// <summary>Movimento do razão de uma conta (com saldo acumulado).</summary>
public record RazaoMovimento(DateTime Data, string Historico, decimal Debito, decimal Credito, decimal SaldoAcumulado);

/// <summary>DRE simplificada.</summary>
public record DreResultado(decimal Receitas, decimal Despesas)
{
    public decimal Resultado => Receitas - Despesas;
}
