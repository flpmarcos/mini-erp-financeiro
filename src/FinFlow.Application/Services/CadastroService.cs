using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

public class CadastroService : ICadastroService
{
    private readonly AppDbContext _db;
    public CadastroService(AppDbContext db) => _db = db;

    public Task<List<Categoria>> ListarCategoriasAsync() =>
        _db.Categorias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();

    public Task<List<CentroCusto>> ListarCentrosAsync() =>
        _db.CentrosCusto.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();

    public Task<List<ContaBancaria>> ListarContasBancariasAsync() =>
        _db.ContasBancarias.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();

    public async Task<Categoria> CriarCategoriaAsync(Categoria c)
    {
        _db.Categorias.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    public async Task<CentroCusto> CriarCentroAsync(CentroCusto c)
    {
        _db.CentrosCusto.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    public async Task<ContaBancaria> CriarContaBancariaAsync(ContaBancaria c)
    {
        _db.ContasBancarias.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }
}
