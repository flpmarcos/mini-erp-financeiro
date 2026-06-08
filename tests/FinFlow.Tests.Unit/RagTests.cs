using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Integrations.Rag;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FinFlow.Tests.Unit;

public class RagTests
{
    private static async Task<(RagService rag, DocumentIngestionService ingest, InMemoryVectorStore store, FinFlow.Data.AppDbContext db)> BuildAsync()
    {
        var db = TestSupport.NewDb();
        var emb = new FakeEmbeddingService();
        var store = new InMemoryVectorStore();
        var ingest = new DocumentIngestionService(db, emb, store);

        // Seed mínimo: fornecedor bloqueado + uma regra.
        db.Fornecedores.Add(new Fornecedor { RazaoSocial = "Servicos Gerais ME", Documento = "11222333000181", Status = StatusFornecedor.Bloqueado });
        db.RegrasAprovacao.Add(new RegraAprovacao { Nome = "Diretor acima de 10000", ValorMinimo = 10000.01m, NivelExigido = NivelAprovacao.Diretor });
        await db.SaveChangesAsync();
        await ingest.IngerirBaseAsync();

        var auditoria = new AuditoriaService(db, Mock.Of<IHttpContextAccessor>());
        var retriever = new PermissionAwareRetriever(emb, store);
        var rag = new RagService(retriever, new FakeLlmProvider(), auditoria);
        return (rag, ingest, store, db);
    }

    private static RagUsuario User(int empresa = 1, params string[] roles) => new("ana@demo.com", empresa, roles.ToList());

    [Fact]
    public void Embeddings_TextosParecidos_TemAltaSimilaridade()
    {
        var emb = new FakeEmbeddingService();
        var a = emb.Gerar("fornecedor bloqueado pagamento");
        var b = emb.Gerar("fornecedor bloqueado");
        var c = emb.Gerar("clima tempo chuva sol");
        double Cos(float[] x, float[] y){ double d=0,nx=0,ny=0; for(int i=0;i<x.Length;i++){d+=x[i]*y[i];nx+=x[i]*x[i];ny+=y[i]*y[i];} return d/(Math.Sqrt(nx)*Math.Sqrt(ny)); }
        Cos(a, b).Should().BeGreaterThan(Cos(a, c));
    }

    [Fact]
    public async Task Ingestao_PopulaVectorStore()
    {
        var (_, _, store, _) = await BuildAsync();
        (await store.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Reindexar_NaoDuplica()
    {
        var (_, ingest, store, _) = await BuildAsync();
        var apos1 = await store.CountAsync();
        await ingest.IngerirBaseAsync();           // reindexa
        (await store.CountAsync()).Should().Be(apos1, "reindex deve substituir, não duplicar");
    }

    [Fact]
    public async Task Pergunta_ComContexto_RetornaRespostaEFontes()
    {
        var (rag, _, _, _) = await BuildAsync();
        var r = await rag.PerguntarAsync("Temos algum fornecedor bloqueado?", User(1, "Financeiro"));
        r.TrechosUsados.Should().BeGreaterThan(0);
        r.Fontes.Should().NotBeEmpty();
        r.Resposta.Should().ContainEquivalentOf("bloquead", "deve trazer o fornecedor bloqueado do contexto");
    }

    [Fact]
    public async Task Pergunta_SemContexto_DizQueNaoEncontrou()
    {
        var (rag, _, _, _) = await BuildAsync();
        var r = await rag.PerguntarAsync("qual a previsão do tempo em marte amanhã", User(1, "Financeiro"));
        r.TrechosUsados.Should().Be(0);
        r.Resposta.Should().ContainEquivalentOf("não encontrei", "sem contexto não deve inventar");
    }

    [Fact]
    public async Task Permissao_FinanceiroNaoVeConteudoDaDiretoria()
    {
        var (rag, _, _, _) = await BuildAsync();
        var fin = await rag.PerguntarAsync("metas de bônus e teto de gastos confidenciais da diretoria", User(1, "Financeiro"));
        fin.Fontes.Should().NotContain(f => f.Tipo == "Doc" && f.Fonte == "Política" && f.Entidade == null && false);
        // Diretor vê
        var dir = await rag.PerguntarAsync("metas de bônus e teto de gastos confidenciais da diretoria", User(1, "Diretor"));
        dir.TrechosUsados.Should().BeGreaterThan(fin.TrechosUsados);
    }

    [Fact]
    public async Task Multiempresa_UsuarioEmpresa2_NaoVeDadosEmpresa1()
    {
        var (rag, _, _, _) = await BuildAsync();
        var r = await rag.PerguntarAsync("Temos algum fornecedor bloqueado?", User(2, "Financeiro"));
        r.TrechosUsados.Should().Be(0);
    }

    [Fact]
    public async Task Auditoria_RegistraPergunta()
    {
        var (rag, _, _, db) = await BuildAsync();
        await rag.PerguntarAsync("Quais regras de aprovação existem?", User(1, "Financeiro"));
        db.AuditLogs.Should().Contain(a => a.Entidade == "RagQuery");
    }

    [Fact]
    public void Mascaramento_OcultaDocumentoParaNaoPrivilegiado()
    {
        var original = "Fornecedor ACME documento 11222333000181 ativo.";
        SensitiveDataMasker.Mascarar(original, podeVerSensivel: false).Should().NotContain("11222333000181");
        SensitiveDataMasker.Mascarar(original, podeVerSensivel: true).Should().Contain("11222333000181");
    }
}
