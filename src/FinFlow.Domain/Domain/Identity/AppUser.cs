using Microsoft.AspNetCore.Identity;

namespace FinFlow.Domain.Identity;

/// <summary>Usuario da aplicacao (estende o IdentityUser com dados de exibicao).</summary>
public class AppUser : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>Empresa (tenant) à qual o usuário pertence.</summary>
    public int EmpresaId { get; set; } = 1;
}

/// <summary>Perfis (roles) do sistema. Centraliza os nomes para evitar string solta.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Financeiro = "Financeiro";
    public const string Gerente = "Gerente";
    public const string Diretor = "Diretor";
    public const string Auditor = "Auditor";

    public static readonly string[] Todos = { Admin, Financeiro, Gerente, Diretor, Auditor };
}

/// <summary>Nomes das policies de autorizacao usadas em controllers/actions.</summary>
public static class Policies
{
    /// <summary>Cadastrar/editar contas, fornecedores, cadastros (Financeiro/Admin).</summary>
    public const string PodeCadastrar = "PodeCadastrar";
    /// <summary>Aprovar/reprovar contas (Gerente/Diretor/Admin).</summary>
    public const string PodeAprovar = "PodeAprovar";
    /// <summary>Efetuar pagamento/baixa (Financeiro/Admin).</summary>
    public const string PodePagar = "PodePagar";
    /// <summary>Ver relatorios/dashboard (todos os perfis, inclui Auditor).</summary>
    public const string PodeVisualizar = "PodeVisualizar";
    /// <summary>Ver a trilha de auditoria (Auditor/Diretor/Admin).</summary>
    public const string PodeAuditar = "PodeAuditar";
    /// <summary>Administracao do sistema (Admin).</summary>
    public const string Administrar = "Administrar";
}
