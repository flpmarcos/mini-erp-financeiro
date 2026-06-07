using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditoriaService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task RegistrarAsync(AcaoAuditoria acao, string entidade, int entidadeId,
        string? campo = null, string? valorAnterior = null, string? valorNovo = null,
        string usuario = "sistema", string? motivo = null)
    {
        await _db.AuditLogs.AddAsync(new AuditLog
        {
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Campo = campo,
            ValorAnterior = valorAnterior,
            ValorNovo = valorNovo,
            Usuario = usuario,
            Motivo = motivo,
            Ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            DataHora = DateTime.UtcNow
        });
        // Nao chama SaveChanges aqui: o service de negocio controla a transacao.
    }

    public Task SalvarPendentesAsync() => _db.SaveChangesAsync();

    public async Task<PagedResult<AuditLog>> ListarAsync(string? entidade, int pagina, int tamanho)
    {
        var query = _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.DataHora).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidade))
            query = query.Where(a => a.Entidade == entidade);

        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PagedResult<AuditLog> { Itens = itens, TotalItens = total, Pagina = pagina, TamanhoPagina = tamanho };
    }
}
