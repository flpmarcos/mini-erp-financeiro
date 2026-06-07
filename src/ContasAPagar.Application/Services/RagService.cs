using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Integrations.Rag;
using ContasAPagar.Web.Services.Interfaces;

namespace ContasAPagar.Web.Services;

/// <summary>Recupera trechos do vector store filtrando por empresa (tenant) e permissão.</summary>
public class PermissionAwareRetriever : IPermissionAwareRetriever
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;

    public PermissionAwareRetriever(IEmbeddingService embeddings, IVectorStore store)
    {
        _embeddings = embeddings;
        _store = store;
    }

    public Task<List<VectorDocument>> RecuperarAsync(string pergunta, RagUsuario usuario, int topK = 5)
    {
        var q = _embeddings.Gerar(pergunta);
        // minScore corta ruído (colisões de hash) → "sem contexto" não inventa resposta.
        return _store.SearchAsync(q, topK,
                d => d.EmpresaId == usuario.EmpresaId && usuario.TemPermissao(d.PermissaoMinima),
                minScore: 0.18)
            .ContinueWith(t => t.Result.Select(x => x.Doc).ToList());
    }
}

/// <summary>
/// Orquestra o RAG: recupera contexto AUTORIZADO (tenant + permissão), mascara dados
/// sensíveis, chama o LLM (que não inventa) e audita a pergunta. Retorna as fontes.
/// </summary>
public class RagService : IRagService
{
    private readonly IPermissionAwareRetriever _retriever;
    private readonly ILlmProvider _llm;
    private readonly IAuditoriaService _auditoria;

    public RagService(IPermissionAwareRetriever retriever, ILlmProvider llm, IAuditoriaService auditoria)
    {
        _retriever = retriever;
        _llm = llm;
        _auditoria = auditoria;
    }

    public async Task<RagResposta> PerguntarAsync(string pergunta, RagUsuario usuario)
    {
        var docs = await _retriever.RecuperarAsync(pergunta, usuario);

        // Mascara dados sensíveis para quem não tem privilégio.
        var contextos = docs.Select(d => SensitiveDataMasker.Mascarar(d.Conteudo, usuario.PodeVerSensivel)).ToList();

        var resposta = await _llm.ResponderAsync(pergunta, contextos);
        var fontes = docs.Select(d => new FonteRag(d.Fonte, d.Tipo, d.EntidadeRelacionada)).ToList();

        // Auditoria da consulta à IA (não registra dados sensíveis no log).
        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, "RagQuery", 0,
            campo: "pergunta", valorNovo: pergunta.Length > 200 ? pergunta[..200] : pergunta, usuario: usuario.Login);
        await _auditoria.SalvarPendentesAsync();

        return new RagResposta(resposta, fontes, contextos.Count);
    }
}
