using System.Text;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Integrations.Storage;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FinFlow.Tests.Unit;

public class AnexoServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "anexo-test-" + Guid.NewGuid());

    private async Task<(AnexoService svc, int contaId)> BuildAsync()
    {
        var db = TestSupport.NewDb();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new ContaPagar
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 100m, ValorLiquido = 100m, DataVencimento = DateTime.Today
        };
        db.ContasPagar.Add(conta);
        await db.SaveChangesAsync();

        var storage = new LocalFileStorage(_tempDir);
        var svc = new AnexoService(db, storage, new Mock<IAuditoriaService>().Object);
        return (svc, conta.Id);
    }

    private static MemoryStream Stream(string conteudo) => new(Encoding.UTF8.GetBytes(conteudo));

    [Fact]
    public async Task Upload_ArquivoValido_Persiste()
    {
        var (svc, contaId) = await BuildAsync();
        using var s = Stream("nota fiscal");
        var r = await svc.UploadAsync(contaId, "nota.pdf", "application/pdf", s.Length, s, TipoAnexo.NotaFiscal, "tester");

        r.Sucesso.Should().BeTrue();
        (await svc.ListarAsync(contaId)).Should().ContainSingle();

        var baixado = await svc.BaixarAsync(r.Dados!.Id);
        baixado.Should().NotBeNull();
        Encoding.UTF8.GetString(baixado!.Conteudo).Should().Be("nota fiscal");
    }

    [Fact]
    public async Task Upload_ExtensaoInvalida_Falha()
    {
        var (svc, contaId) = await BuildAsync();
        using var s = Stream("x");
        var r = await svc.UploadAsync(contaId, "virus.exe", "application/octet-stream", s.Length, s, TipoAnexo.Outro, "tester");
        r.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Upload_ArquivoGrande_Falha()
    {
        var (svc, contaId) = await BuildAsync();
        using var s = new MemoryStream(new byte[6 * 1024 * 1024]); // 6 MB
        var r = await svc.UploadAsync(contaId, "grande.pdf", "application/pdf", s.Length, s, TipoAnexo.Outro, "tester");
        r.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Remover_ApagaRegistro()
    {
        var (svc, contaId) = await BuildAsync();
        using var s = Stream("x");
        var anexo = (await svc.UploadAsync(contaId, "a.txt", "text/plain", s.Length, s, TipoAnexo.Outro, "t")).Dados!;

        var r = await svc.RemoverAsync(anexo.Id, "t");
        r.Sucesso.Should().BeTrue();
        (await svc.ListarAsync(contaId)).Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }
}
