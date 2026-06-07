using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Repositories;
using ContasAPagar.Web.Services;
using FluentAssertions;

namespace AccountsPayable.Tests.Unit;

public class FornecedorServiceTests
{
    private static FornecedorService Svc(out ContasAPagar.Web.Data.AppDbContext db)
    {
        db = TestSupport.NewDb();
        return new FornecedorService(new EfRepository<Fornecedor>(db));
    }

    [Fact]
    public async Task CriarAsync_SemRazaoSocial_Falha()
    {
        var svc = Svc(out _);
        var r = await svc.CriarAsync(new Fornecedor { RazaoSocial = "", Documento = "11222333000181" });
        r.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task CriarAsync_DocumentoInvalido_Falha()
    {
        var svc = Svc(out _);
        var r = await svc.CriarAsync(new Fornecedor
        {
            RazaoSocial = "ACME", TipoDocumento = TipoDocumento.Cnpj, Documento = "00000000000000"
        });
        r.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task CriarAsync_Valido_Persiste()
    {
        var svc = Svc(out _);
        var r = await svc.CriarAsync(new Fornecedor
        {
            RazaoSocial = "ACME LTDA", TipoDocumento = TipoDocumento.Cnpj, Documento = "11.222.333/0001-81"
        });
        r.Sucesso.Should().BeTrue();
        r.Dados!.Id.Should().BeGreaterThan(0);
        r.Dados.Documento.Should().Be("11222333000181"); // normalizado
    }

    [Fact]
    public async Task CriarAsync_DocumentoDuplicado_Falha()
    {
        var svc = Svc(out _);
        await svc.CriarAsync(new Fornecedor { RazaoSocial = "A", Documento = "11222333000181" });
        var r = await svc.CriarAsync(new Fornecedor { RazaoSocial = "B", Documento = "11222333000181" });
        r.Sucesso.Should().BeFalse();
    }
}
