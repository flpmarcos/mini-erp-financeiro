using FinFlow.Domain.Entities;

namespace FinFlow.Services.Interfaces;

/// <summary>Cadastros auxiliares: categorias, centros de custo e contas bancarias.</summary>
public interface ICadastroService
{
    Task<List<Categoria>> ListarCategoriasAsync();
    Task<List<CentroCusto>> ListarCentrosAsync();
    Task<List<ContaBancaria>> ListarContasBancariasAsync();

    Task<Categoria> CriarCategoriaAsync(Categoria c);
    Task<CentroCusto> CriarCentroAsync(CentroCusto c);
    Task<ContaBancaria> CriarContaBancariaAsync(ContaBancaria c);
}
