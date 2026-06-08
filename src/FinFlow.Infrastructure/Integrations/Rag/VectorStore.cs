using System.Collections.Concurrent;

namespace FinFlow.Integrations.Rag;

/// <summary>
/// Documento vetorizado no índice (chunk + embedding + metadados de permissão/tenant).
/// PermissaoMinima: role mínima exigida ("*" = todos os autenticados).
/// </summary>
public class VectorDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Conteudo { get; init; } = string.Empty;
    public float[] Vetor { get; set; } = Array.Empty<float>();

    public string Fonte { get; init; } = string.Empty;          // README, Banco, Política...
    public string Tipo { get; init; } = string.Empty;           // ContaPagar, Fornecedor, Regra, Doc...
    public string? EntidadeRelacionada { get; init; }            // "ContaPagar:52"
    public int EmpresaId { get; init; } = 1;
    public string Area { get; init; } = "Financeiro";
    public string PermissaoMinima { get; init; } = "*";
    public DateTime DataCriacao { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Vector store. Implementação atual em memória; troque por pgvector/Qdrant
/// criando outra implementação desta interface (ver README).
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(IEnumerable<VectorDocument> docs);
    Task<List<(VectorDocument Doc, double Score)>> SearchAsync(float[] query, int topK, Func<VectorDocument, bool>? filtro = null, double minScore = 0);
    Task<int> CountAsync();
    Task ClearAsync();
}

/// <summary>Vector store em memória (singleton). Similaridade por cosseno.</summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, VectorDocument> _docs = new();

    public Task UpsertAsync(IEnumerable<VectorDocument> docs)
    {
        foreach (var d in docs) _docs[d.Id] = d;
        return Task.CompletedTask;
    }

    public Task<List<(VectorDocument, double)>> SearchAsync(float[] query, int topK, Func<VectorDocument, bool>? filtro = null, double minScore = 0)
    {
        var resultado = _docs.Values
            .Where(d => filtro is null || filtro(d))
            .Select(d => (d, Score: Cosseno(query, d.Vetor)))
            .Where(x => x.Score > minScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
        return Task.FromResult(resultado);
    }

    public Task<int> CountAsync() => Task.FromResult(_docs.Count);
    public Task ClearAsync() { _docs.Clear(); return Task.CompletedTask; }

    private static double Cosseno(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
