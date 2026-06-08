namespace FinFlow.ViewModels;

/// <summary>Projeção de caixa para um horizonte (ex.: 7, 30, 90 dias).</summary>
public class HorizonteFluxo
{
    public int Dias { get; set; }
    public decimal EntradasPrevistas { get; set; }
    public decimal SaidasPrevistas { get; set; }
    public decimal SaldoProjetado { get; set; }
    public decimal ResultadoPrevisto => EntradasPrevistas - SaidasPrevistas;
}

public class FluxoCaixaVM
{
    public decimal SaldoAtual { get; set; }
    public decimal TotalRecebidoRealizado { get; set; }
    public decimal TotalPagoRealizado { get; set; }
    public List<HorizonteFluxo> Horizontes { get; set; } = new();
}
