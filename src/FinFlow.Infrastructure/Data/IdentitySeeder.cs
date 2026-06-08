using FinFlow.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace FinFlow.Data;

/// <summary>
/// Cria roles e usuarios de teste (um por perfil). Idempotente.
/// Senha padrao de estudo: Senha123! (troque em producao).
/// </summary>
public static class IdentitySeeder
{
    public const string SenhaPadrao = "Senha123!";

    private static readonly (string email, string nome, string role)[] Usuarios =
    {
        ("admin@demo.com",      "Administrador",   Roles.Admin),
        ("financeiro@demo.com", "Ana Financeiro",  Roles.Financeiro),
        ("gerente@demo.com",    "Bruno Gerente",   Roles.Gerente),
        ("diretor@demo.com",    "Carla Diretora",  Roles.Diretor),
        ("auditor@demo.com",    "Diego Auditor",   Roles.Auditor),
    };

    public static async Task SeedAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in Roles.Todos)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        foreach (var (email, nome, role) in Usuarios)
        {
            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var user = new AppUser { UserName = email, Email = email, NomeCompleto = nome, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, SenhaPadrao);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
