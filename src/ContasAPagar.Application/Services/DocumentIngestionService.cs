using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Integrations.Rag;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Ingestão de conhecimento para o RAG: contas, fornecedores, regras de aprovação e
/// políticas internas → chunks → embeddings → vector store (com metadados).
/// </summary>
public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _store;

    public DocumentIngestionService(AppDbContext db, IEmbeddingService embeddings, IVectorStore store)
    {
        _db = db;
        _embeddings = embeddings;
        _store = store;
    }

    public async Task<int> IngerirBaseAsync()
    {
        var docs = new List<VectorDocument>();

        // Contas a pagar (exclui conta-mãe de parcelamento).
        var contas = await _db.ContasPagar.AsNoTracking().Include(c => c.Fornecedor).Include(c => c.Categoria)
            .Where(c => c.NumeroParcela != 0).ToListAsync();
        foreach (var c in contas)
            docs.Add(Doc(
                $"Conta a pagar #{c.Id}: {c.Descricao}. Fornecedor {c.Fornecedor?.RazaoSocial}. Categoria {c.Categoria?.Nome}. " +
                $"Valor líquido R$ {c.ValorLiquido:F2}, vence em {c.DataVencimento:dd/MM/yyyy}, status {c.Status}.",
                "Banco", "ContaPagar", $"ContaPagar:{c.Id}", c.EmpresaId, "Financeiro", "*"));

        // Fornecedores (status bloqueado é relevante).
        foreach (var f in await _db.Fornecedores.AsNoTracking().ToListAsync())
            docs.Add(Doc(
                $"Fornecedor {f.RazaoSocial} (documento {f.Documento}) está {f.Status}.",
                "Banco", "Fornecedor", $"Fornecedor:{f.Id}", f.EmpresaId, "Financeiro", "*"));

        // Regras de aprovação.
        foreach (var r in await _db.RegrasAprovacao.AsNoTracking().ToListAsync())
            docs.Add(Doc(
                $"Regra de aprovação '{r.Nome}': faixa de R$ {r.ValorMinimo:F2} a " +
                $"{(r.ValorMaximo.HasValue ? $"R$ {r.ValorMaximo:F2}" : "sem teto")} exige nível {r.NivelExigido}.",
                "Política", "Regra", $"RegraAprovacao:{r.Id}", r.EmpresaId, "Financeiro", "*"));

        // Políticas internas (texto fixo). Uma restrita à Diretoria.
        docs.Add(Doc("Política de pagamentos: contas acima de R$ 10.000 exigem aprovação de diretor; entre R$ 1.000 e R$ 10.000, de gerente; abaixo de R$ 1.000, aprovação automática.",
            "Política", "Doc", null, 1, "Financeiro", "*"));
        docs.Add(Doc("Política de conciliação: pagamentos são conciliados com o extrato bancário por valor e data, com tolerância de 3 dias.",
            "Política", "Doc", null, 1, "Financeiro", "*"));
        docs.Add(Doc("Política restrita da Diretoria: metas de bônus e teto de gastos estratégicos do trimestre são confidenciais da diretoria.",
            "Política", "Doc", null, 1, "Diretoria", "Diretor"));

        foreach (var d in docs) d.Vetor = _embeddings.Gerar(d.Conteudo);
        await _store.UpsertAsync(docs);
        return docs.Count;
    }

    public async Task<int> IngerirDocumentoAsync(string fonte, string conteudo, string permissaoMinima = "*", int empresaId = 1)
    {
        var chunks = Chunk(conteudo);
        var docs = chunks.Select(ch =>
        {
            var d = Doc(ch, fonte, "Doc", null, empresaId, "Financeiro", permissaoMinima);
            d.Vetor = _embeddings.Gerar(ch);
            return d;
        }).ToList();
        await _store.UpsertAsync(docs);
        return docs.Count;
    }

    private static VectorDocument Doc(string conteudo, string fonte, string tipo, string? entidade, int empresaId, string area, string perm) =>
        new() { Conteudo = conteudo, Fonte = fonte, Tipo = tipo, EntidadeRelacionada = entidade, EmpresaId = empresaId, Area = area, PermissaoMinima = perm };

    private static IEnumerable<string> Chunk(string texto)
    {
        // Quebra simples por parágrafos, ignorando trechos muito curtos.
        return (texto ?? string.Empty)
            .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 40)
            .Take(200);
    }
}
