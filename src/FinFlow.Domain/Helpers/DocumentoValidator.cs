namespace FinFlow.Helpers;

/// <summary>
/// Validacao simples de CPF/CNPJ (estrutura + digitos verificadores).
/// Suficiente para estudo; em producao considere lib dedicada.
/// </summary>
public static class DocumentoValidator
{
    public static string SomenteDigitos(string? valor) =>
        new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool ValidarCpf(string? cpf)
    {
        cpf = SomenteDigitos(cpf);
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;

        int Calc(int len)
        {
            int soma = 0, peso = len + 1;
            for (int i = 0; i < len; i++) soma += (cpf[i] - '0') * peso--;
            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Calc(9) == cpf[9] - '0' && Calc(10) == cpf[10] - '0';
    }

    public static bool ValidarCnpj(string? cnpj)
    {
        cnpj = SomenteDigitos(cnpj);
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;

        int Calc(int len)
        {
            int[] peso = len == 12
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma = 0;
            for (int i = 0; i < len; i++) soma += (cnpj[i] - '0') * peso[i];
            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Calc(12) == cnpj[12] - '0' && Calc(13) == cnpj[13] - '0';
    }
}
