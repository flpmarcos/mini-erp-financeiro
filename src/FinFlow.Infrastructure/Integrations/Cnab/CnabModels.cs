namespace FinFlow.Integrations.Cnab;

/// <summary>Resumo do processamento de um arquivo de retorno CNAB (fake).</summary>
public record ResultadoRetornoCnab(int Processados, int Confirmados, int Rejeitados, int Ignorados);

/// <summary>
/// Formato CNAB DIDÁTICO (não é CNAB 240/400 real — simplificado para estudo).
/// Remessa (linha por conta):  R{contaId:8}{valorCentavos:13}
/// Retorno (linha por conta):  T{contaId:8}{valorCentavos:13}{codigo:2}
///   codigo 00 = pagamento confirmado · 09 = rejeitado · demais = ignorado
/// </summary>
public static class CnabLayout
{
    public const char TipoRemessa = 'R';
    public const char TipoRetorno = 'T';
    public const string CodConfirmado = "00";
    public const string CodRejeitado = "09";

    public static string LinhaRemessa(int contaId, decimal valor) =>
        $"{TipoRemessa}{contaId:D8}{(long)Math.Round(valor * 100):D13}";

    public static bool TryParseRetorno(string linha, out int contaId, out decimal valor, out string codigo)
    {
        contaId = 0; valor = 0m; codigo = string.Empty;
        if (string.IsNullOrWhiteSpace(linha) || linha.Length < 24 || linha[0] != TipoRetorno) return false;

        if (!int.TryParse(linha.Substring(1, 8), out contaId)) return false;
        if (!long.TryParse(linha.Substring(9, 13), out var cents)) return false;
        valor = cents / 100m;
        codigo = linha.Substring(22, 2);
        return true;
    }
}
