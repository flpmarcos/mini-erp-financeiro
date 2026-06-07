using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace ContasAPagar.Web.Integrations.Conciliacao;

/// <summary>Linha bruta do CSV de extrato bancario (delimitador ';').</summary>
public class ExtratoCsvRow
{
    [Name("Data")]
    public string Data { get; set; } = string.Empty;
    [Name("Descricao")]
    public string Descricao { get; set; } = string.Empty;
    [Name("Valor")]
    public string Valor { get; set; } = string.Empty;
    [Name("Documento")]
    public string? Documento { get; set; }
    [Name("Banco")]
    public string? Banco { get; set; }
    [Name("Tipo")]
    public string? Tipo { get; set; }
}

/// <summary>Mapa CsvHelper - tolerante a cabecalhos com espacos/maiusculas.</summary>
public sealed class ExtratoCsvMap : ClassMap<ExtratoCsvRow>
{
    public ExtratoCsvMap()
    {
        Map(m => m.Data).Name("Data");
        Map(m => m.Descricao).Name("Descricao", "Descrição");
        Map(m => m.Valor).Name("Valor");
        Map(m => m.Documento).Name("Documento").Optional();
        Map(m => m.Banco).Name("Banco").Optional();
        Map(m => m.Tipo).Name("Tipo").Optional();
    }
}
