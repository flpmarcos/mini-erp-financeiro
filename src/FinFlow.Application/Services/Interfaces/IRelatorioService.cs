using FinFlow.Domain.Entities;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface IRelatorioService
{
    Task<List<ContaPagar>> ContasVencidasAsync();
    Task<List<ContaPagar>> AVencer7DiasAsync();
    Task<List<ContaPagar>> PagasNoMesAsync();
    Task<List<ContaPagar>> PendentesAsync();
    Task<List<ContaPagar>> ParcialmentePagasAsync();

    Task<List<GraficoItem>> FluxoCaixaPrevistoAsync();
    Task<List<GraficoItem>> DespesasPorFornecedorAsync();
    Task<List<GraficoItem>> DespesasPorCentroCustoAsync();
    Task<List<GraficoItem>> DespesasPorCategoriaAsync();
    Task<List<GraficoItem>> ImpostosRetidosAsync();
    Task<List<GraficoItem>> PagamentosPorBancoAsync();
}
