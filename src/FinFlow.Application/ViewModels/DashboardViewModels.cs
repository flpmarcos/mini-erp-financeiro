using FinFlow.Domain.Entities;

namespace FinFlow.ViewModels;

public class GraficoItem
{
    public string Rotulo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public class DashboardVM
{
    public decimal TotalAPagarMes { get; set; }
    public decimal TotalVencido { get; set; }
    public decimal TotalPagoMes { get; set; }
    public decimal TotalPendente { get; set; }
    public decimal TotalEmAprovacao { get; set; }
    public decimal TotalParcialmentePago { get; set; }

    public List<GraficoItem> PorCategoria { get; set; } = new();
    public List<GraficoItem> PorCentroCusto { get; set; } = new();

    public List<ContaPagar> Vencidas { get; set; } = new();
    public List<ContaPagar> ProximasDoVencimento { get; set; } = new();
}
