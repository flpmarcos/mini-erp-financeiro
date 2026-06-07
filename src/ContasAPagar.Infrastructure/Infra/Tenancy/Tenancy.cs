using System.Security.Claims;
using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ContasAPagar.Web.Infrastructure.Tenancy;

/// <summary>Claim que carrega a empresa (tenant) do usuário autenticado.</summary>
public static class TenantClaims
{
    public const string EmpresaId = "EmpresaId";
}

/// <summary>Adiciona o claim EmpresaId ao principal no login.</summary>
public class TenantClaimsFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public TenantClaimsFactory(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(TenantClaims.EmpresaId, user.EmpresaId.ToString()));
        return identity;
    }
}

/// <summary>
/// Middleware que define a empresa atual no AppDbContext (filtro global) a partir
/// do claim do usuário. Deve rodar após UseAuthentication.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        var claim = context.User.FindFirst(TenantClaims.EmpresaId)?.Value;
        if (int.TryParse(claim, out var empresaId))
            db.EmpresaIdFiltro = empresaId;

        await _next(context);
    }
}
