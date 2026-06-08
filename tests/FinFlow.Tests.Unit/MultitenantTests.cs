using FinFlow.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Tests.Unit;

public class MultitenantTests
{
    [Fact]
    public async Task QueryFilter_IsolaPorEmpresa()
    {
        var db = TestSupport.NewDb();
        db.Fornecedores.AddRange(
            new Fornecedor { RazaoSocial = "A", Documento = "11222333000181", EmpresaId = 1 },
            new Fornecedor { RazaoSocial = "B", Documento = "44555666000199", EmpresaId = 2 });
        await db.SaveChangesAsync();

        db.EmpresaIdFiltro = 1;
        (await db.Fornecedores.ToListAsync()).Should().OnlyContain(f => f.EmpresaId == 1);

        db.EmpresaIdFiltro = 2;
        (await db.Fornecedores.ToListAsync()).Should().OnlyContain(f => f.EmpresaId == 2);
    }

    [Fact]
    public async Task SemFiltro_VeTodasAsEmpresas()
    {
        var db = TestSupport.NewDb();
        db.Fornecedores.AddRange(
            new Fornecedor { RazaoSocial = "A", Documento = "11222333000181", EmpresaId = 1 },
            new Fornecedor { RazaoSocial = "B", Documento = "44555666000199", EmpresaId = 2 });
        await db.SaveChangesAsync();

        db.EmpresaIdFiltro = null;
        (await db.Fornecedores.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SaveChanges_CarimbaEmpresaAtual()
    {
        var db = TestSupport.NewDb();
        db.EmpresaIdFiltro = 2;
        db.Fornecedores.Add(new Fornecedor { RazaoSocial = "Nova", Documento = "11222333000181" });
        await db.SaveChangesAsync();

        db.EmpresaIdFiltro = null;
        (await db.Fornecedores.SingleAsync()).EmpresaId.Should().Be(2);
    }
}
