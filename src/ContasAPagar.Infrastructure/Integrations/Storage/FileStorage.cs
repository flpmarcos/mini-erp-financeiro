namespace ContasAPagar.Web.Integrations.Storage;

/// <summary>
/// Porta de armazenamento de arquivos. Implementação atual = disco local.
/// Trocar por S3/MinIO no futuro = criar outra implementação desta interface.
/// </summary>
public interface IFileStorage
{
    /// <summary>Salva o conteúdo e retorna o caminho relativo persistido.</summary>
    Task<string> SaveAsync(string subdir, string fileName, Stream content, CancellationToken ct = default);
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default);
    void Delete(string relativePath);
}

/// <summary>Armazenamento em disco sob um diretório base (fora de wwwroot).</summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _baseDir;

    public LocalFileStorage(string baseDir)
    {
        _baseDir = baseDir;
        Directory.CreateDirectory(_baseDir);
    }

    public async Task<string> SaveAsync(string subdir, string fileName, Stream content, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(fileName);
        var relative = Path.Combine(subdir, safeName).Replace('\\', '/');
        var full = Path.Combine(_baseDir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using var fs = new FileStream(full, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
        return relative;
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_baseDir, relativePath);
        return File.Exists(full) ? await File.ReadAllBytesAsync(full, ct) : null;
    }

    public void Delete(string relativePath)
    {
        var full = Path.Combine(_baseDir, relativePath);
        if (File.Exists(full)) File.Delete(full);
    }
}
