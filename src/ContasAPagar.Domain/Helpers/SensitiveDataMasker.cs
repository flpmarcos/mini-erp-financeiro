using System.Text.RegularExpressions;

namespace ContasAPagar.Web.Helpers;

/// <summary>Mascara dados sensíveis (CPF/CNPJ) em textos quando o usuário não tem privilégio.</summary>
public static class SensitiveDataMasker
{
    private static readonly Regex Documentos = new(@"\b\d{11,14}\b", RegexOptions.Compiled);

    public static string Mascarar(string texto, bool podeVerSensivel)
    {
        if (podeVerSensivel || string.IsNullOrEmpty(texto)) return texto;
        return Documentos.Replace(texto, m =>
        {
            var d = m.Value;
            return d.Length <= 4 ? new string('*', d.Length) : new string('*', d.Length - 4) + d[^4..];
        });
    }
}
