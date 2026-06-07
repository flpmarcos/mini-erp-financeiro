using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Regras de aprovação configuráveis. A resolução escolhe a regra ATIVA mais
/// específica (mais filtros casados) que cobre o valor; empate → maior alçada.
/// </summary>
public class RegraAprovacaoService : IRegraAprovacaoService
{
    private readonly AppDbContext _db;
    public RegraAprovacaoService(AppDbContext db) => _db = db;

    public Task<List<RegraAprovacao>> ListarAsync() =>
        _db.RegrasAprovacao.AsNoTracking()
            .Include(r => r.Categoria).Include(r => r.CentroCusto).Include(r => r.Fornecedor)
            .OrderByDescending(r => r.Ativa).ThenBy(r => r.ValorMinimo).ToListAsync();

    public Task<RegraAprovacao?> ObterAsync(int id) => _db.RegrasAprovacao.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<OperationResult<RegraAprovacao>> CriarAsync(RegraAprovacao r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return OperationResult<RegraAprovacao>.Falha("Nome e obrigatorio.");
        if (r.ValorMaximo.HasValue && r.ValorMaximo < r.ValorMinimo)
            return OperationResult<RegraAprovacao>.Falha("Valor maximo menor que o minimo.");
        _db.RegrasAprovacao.Add(r);
        await _db.SaveChangesAsync();
        return OperationResult<RegraAprovacao>.Ok(r);
    }

    public async Task<OperationResult> AtualizarAsync(RegraAprovacao r)
    {
        var atual = await _db.RegrasAprovacao.FindAsync(r.Id);
        if (atual is null) return OperationResult.Falha("Regra nao encontrada.");
        atual.Nome = r.Nome;
        atual.Ativa = r.Ativa;
        atual.ValorMinimo = r.ValorMinimo;
        atual.ValorMaximo = r.ValorMaximo;
        atual.CategoriaId = r.CategoriaId;
        atual.CentroCustoId = r.CentroCustoId;
        atual.FornecedorId = r.FornecedorId;
        atual.NivelExigido = r.NivelExigido;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoverAsync(int id)
    {
        var r = await _db.RegrasAprovacao.FindAsync(id);
        if (r is null) return OperationResult.Falha("Regra nao encontrada.");
        _db.RegrasAprovacao.Remove(r);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<NivelAprovacao?> ResolverNivelAsync(decimal valor, int categoriaId, int centroCustoId, int fornecedorId)
    {
        var regras = await _db.RegrasAprovacao.AsNoTracking().Where(r => r.Ativa).ToListAsync();
        var match = regras
            .Where(r => r.Casa(valor, categoriaId, centroCustoId, fornecedorId))
            .OrderByDescending(r => r.Especificidade)
            .ThenByDescending(r => r.NivelExigido)
            .FirstOrDefault();
        return match?.NivelExigido;
    }
}
