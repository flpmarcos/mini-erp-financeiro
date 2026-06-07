using System.Text;
using System.Text.RegularExpressions;

namespace ContasAPagar.Web.Integrations.Rag;

/// <summary>
/// Gera embeddings de texto. Implementação FAKE (determinística, local, sem custo):
/// bag-of-words com hashing em buckets + normalização L2. Suficiente para recuperar
/// trechos com sobreposição de termos. Troque por OpenAI/Azure/local (ver README).
/// </summary>
public interface IEmbeddingService
{
    float[] Gerar(string texto);
}

public class FakeEmbeddingService : IEmbeddingService
{
    private const int Dim = 512;
    private static readonly Regex Tokenizer = new(@"[a-zA-Z0-9áéíóúâêôãõçà]+", RegexOptions.Compiled);

    public float[] Gerar(string texto)
    {
        var v = new float[Dim];
        foreach (Match t in Tokenizer.Matches((texto ?? string.Empty).ToLowerInvariant()))
        {
            var token = t.Value;
            if (token.Length < 2) continue;
            var bucket = (int)((uint)token.GetHashCode() % Dim);
            v[bucket] += 1f;
        }
        // Normaliza L2.
        double norma = Math.Sqrt(v.Sum(x => (double)x * x));
        if (norma > 0) for (int i = 0; i < Dim; i++) v[i] = (float)(v[i] / norma);
        return v;
    }
}

/// <summary>
/// Provedor de LLM. Implementação FAKE EXTRATIVA (não inventa): compõe a resposta a
/// partir dos trechos recuperados. Sem contexto → diz que não encontrou.
/// Troque por OpenAI/Azure OpenAI/modelo local implementando esta interface (ver README).
/// </summary>
public interface ILlmProvider
{
    string Nome { get; }
    Task<string> ResponderAsync(string pergunta, IReadOnlyList<string> contextos);
}

public class FakeLlmProvider : ILlmProvider
{
    public string Nome => "fake-extractive";

    public Task<string> ResponderAsync(string pergunta, IReadOnlyList<string> contextos)
    {
        if (contextos.Count == 0)
            return Task.FromResult("Não encontrei informações suficientes nos dados autorizados para responder a essa pergunta.");

        var sb = new StringBuilder();
        sb.AppendLine("Com base nos dados encontrados:");
        foreach (var c in contextos.Take(4))
            sb.AppendLine($"• {c.Trim()}");
        sb.AppendLine();
        sb.Append("(Resposta extraída do contexto recuperado — assistente local, não inventa dados.)");
        return Task.FromResult(sb.ToString());
    }
}
