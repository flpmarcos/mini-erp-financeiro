using ContasAPagar.Web.ViewModels;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardVM> ObterAsync();
}
