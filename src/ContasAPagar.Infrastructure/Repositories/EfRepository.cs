using System.Linq.Expressions;
using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Repositories;

/// <summary>Implementacao do repositorio generico sobre o EF Core.</summary>
public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public EfRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public IQueryable<T> Query(bool tracking = true) =>
        tracking ? _set : _set.AsNoTracking();

    public Task<T?> GetByIdAsync(int id) => _set.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<T>> ListAsync(Expression<Func<T, bool>>? filtro = null) =>
        filtro is null ? await _set.ToListAsync() : await _set.Where(filtro).ToListAsync();

    public async Task AddAsync(T entidade) => await _set.AddAsync(entidade);
    public void Update(T entidade) => _set.Update(entidade);
    public void Remove(T entidade) => _set.Remove(entidade);
    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}
