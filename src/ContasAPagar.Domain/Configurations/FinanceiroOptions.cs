namespace ContasAPagar.Web.Configurations;

/// <summary>
/// Parametros financeiros configuraveis via appsettings (secao "Financeiro").
/// Centraliza multa/juros e alcadas de aprovacao - evita "numeros magicos" no codigo.
/// </summary>
public class FinanceiroOptions
{
    public const string SectionName = "Financeiro";

    /// <summary>Multa percentual aplicada uma vez sobre conta vencida (ex: 2.0 = 2%).</summary>
    public decimal MultaPercentual { get; set; } = 2.0m;

    /// <summary>Juros ao dia sobre conta vencida (ex: 0.033 = 0,033%/dia ~ 1% ao mes).</summary>
    public decimal JurosDiarioPercentual { get; set; } = 0.033m;

    /// <summary>Teto da aprovacao automatica. Abaixo disso aprova sozinho.</summary>
    public decimal LimiteAprovacaoAutomatica { get; set; } = 1000m;

    /// <summary>Teto da alcada de gerente. Acima disso exige diretor.</summary>
    public decimal LimiteAprovacaoGerente { get; set; } = 10000m;
}
