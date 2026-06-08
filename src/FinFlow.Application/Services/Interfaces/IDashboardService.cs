using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardVM> ObterAsync();
}
