using FinFlow.Domain.Identity;
using Hangfire.Dashboard;

namespace FinFlow.Infrastructure.Jobs;

/// <summary>Restringe o painel /hangfire a usuários autenticados com perfil Admin.</summary>
public class HangfireDashboardAuth : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Admin);
    }
}
