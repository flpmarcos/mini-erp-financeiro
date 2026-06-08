using FinFlow.Integrations.Rag;

namespace FinFlow.Services.Interfaces;

/// <summary>Contexto de permissão do usuário que faz a pergunta ao RAG.</summary>
public record RagUsuario(string Login, int EmpresaId, IReadOnlyCollection<string> Roles)
{
    public bool PodeVerSensivel => Roles.Contains("Admin") || Roles.Contains("Diretor");
    public bool TemPermissao(string permissaoMinima) =>
        permissaoMinima == "*" || Roles.Contains("Admin") || Roles.Contains(permissaoMinima);
}

public record FonteRag(string Fonte, string Tipo, string? Entidade);
public record RagResposta(string Resposta, IReadOnlyList<FonteRag> Fontes, int TrechosUsados);

/// <summary>Lê dados internos (banco + políticas + docs), gera embeddings e indexa.</summary>
public interface IDocumentIngestionService
{
    /// <summary>Indexa contas, fornecedores, regras e políticas. Retorna nº de chunks indexados.</summary>
    Task<int> IngerirBaseAsync();
    /// <summary>Indexa um documento de texto (ex.: README), quebrando em chunks.</summary>
    Task<int> IngerirDocumentoAsync(string fonte, string conteudo, string permissaoMinima = "*", int empresaId = 1);
}

/// <summary>Recupera trechos relevantes respeitando empresa (tenant) e permissão do usuário.</summary>
public interface IPermissionAwareRetriever
{
    Task<List<VectorDocument>> RecuperarAsync(string pergunta, RagUsuario usuario, int topK = 5);
}

/// <summary>Orquestra o RAG: recupera contexto autorizado, mascara, chama o LLM e audita.</summary>
public interface IRagService
{
    Task<RagResposta> PerguntarAsync(string pergunta, RagUsuario usuario);
}
