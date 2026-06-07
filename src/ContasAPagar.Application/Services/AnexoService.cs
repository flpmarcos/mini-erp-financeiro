using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Integrations.Storage;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Gestão de anexos: valida extensão/tamanho, persiste no storage e registra o metadado.
/// </summary>
public class AnexoService : IAnexoService
{
    private static readonly string[] ExtensoesPermitidas = { ".pdf", ".png", ".jpg", ".jpeg", ".xml", ".txt" };
    private const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditoriaService _auditoria;

    public AnexoService(AppDbContext db, IFileStorage storage, IAuditoriaService auditoria)
    {
        _db = db;
        _storage = storage;
        _auditoria = auditoria;
    }

    public Task<List<Anexo>> ListarAsync(int contaPagarId) =>
        _db.Anexos.AsNoTracking().Where(a => a.ContaPagarId == contaPagarId)
            .OrderByDescending(a => a.CriadoEm).ToListAsync();

    public async Task<OperationResult<Anexo>> UploadAsync(int contaPagarId, string nomeArquivo, string contentType,
        long tamanho, Stream conteudo, TipoAnexo tipo, string usuario)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo) || tamanho <= 0)
            return OperationResult<Anexo>.Falha("Arquivo invalido.");
        if (tamanho > TamanhoMaximoBytes)
            return OperationResult<Anexo>.Falha("Arquivo excede o limite de 5 MB.");

        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(ext))
            return OperationResult<Anexo>.Falha($"Extensao '{ext}' nao permitida. Use: {string.Join(", ", ExtensoesPermitidas)}.");

        if (!await _db.ContasPagar.AnyAsync(c => c.Id == contaPagarId))
            return OperationResult<Anexo>.Falha("Conta nao encontrada.");

        var nomeUnico = $"{Guid.NewGuid():N}{ext}";
        var caminho = await _storage.SaveAsync($"contas/{contaPagarId}", nomeUnico, conteudo);

        var anexo = new Anexo
        {
            ContaPagarId = contaPagarId,
            Tipo = tipo,
            NomeArquivo = Path.GetFileName(nomeArquivo),
            CaminhoRelativo = caminho,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Tamanho = tamanho,
            EnviadoPor = usuario
        };
        _db.Anexos.Add(anexo);
        await _auditoria.RegistrarAsync(AcaoAuditoria.EdicaoValor, nameof(Anexo), contaPagarId,
            "Anexo", null, anexo.NomeArquivo, usuario);
        await _db.SaveChangesAsync();
        return OperationResult<Anexo>.Ok(anexo);
    }

    public async Task<ArquivoBaixado?> BaixarAsync(int anexoId)
    {
        var anexo = await _db.Anexos.FindAsync(anexoId);
        if (anexo is null) return null;
        var bytes = await _storage.ReadAsync(anexo.CaminhoRelativo);
        return bytes is null ? null : new ArquivoBaixado(bytes, anexo.NomeArquivo, anexo.ContentType);
    }

    public async Task<OperationResult> RemoverAsync(int anexoId, string usuario)
    {
        var anexo = await _db.Anexos.FindAsync(anexoId);
        if (anexo is null) return OperationResult.Falha("Anexo nao encontrado.");
        _storage.Delete(anexo.CaminhoRelativo);
        _db.Anexos.Remove(anexo);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
