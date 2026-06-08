using System.Linq.Expressions;
using FinFlow.Domain.Entities;

namespace FinFlow.Repositories;

/// <summary>
/// Repositorio generico (demonstracao do padrao Repository).
/// Observacao didatica: o proprio DbContext do EF Core ja e um Unit of Work +
/// Repository. Esta abstracao existe para fins de estudo e para isolar testes;
/// varios services consultam o DbContext diretamente, o que tambem e idiomatico.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    IQueryable<T> Query(bool tracking = true);
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? filtro = null);
    Task AddAsync(T entidade);
    void Update(T entidade);
    void Remove(T entidade);
    Task<int> SaveChangesAsync();
}
