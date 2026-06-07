using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services;
using FluentAssertions;

namespace AccountsPayable.Tests.Unit;

public class RegraAprovacaoServiceTests
{
    private static async Task<RegraAprovacaoService> ComRegrasAsync(ContasAPagar.Web.Data.AppDbContext db, params RegraAprovacao[] regras)
    {
        db.RegrasAprovacao.AddRange(regras);
        await db.SaveChangesAsync();
        return new RegraAprovacaoService(db);
    }

    [Fact]
    public async Task Resolver_FaixaDeValor_RetornaNivel()
    {
        var db = TestSupport.NewDb();
        var svc = await ComRegrasAsync(db,
            new RegraAprovacao { Nome = "auto", ValorMinimo = 0, ValorMaximo = 999.99m, NivelExigido = NivelAprovacao.Automatica },
            new RegraAprovacao { Nome = "ger", ValorMinimo = 1000, ValorMaximo = 10000, NivelExigido = NivelAprovacao.Gerente });

        (await svc.ResolverNivelAsync(500m, 1, 1, 1)).Should().Be(NivelAprovacao.Automatica);
        (await svc.ResolverNivelAsync(5000m, 1, 1, 1)).Should().Be(NivelAprovacao.Gerente);
    }

    [Fact]
    public async Task Resolver_RegraEspecificaPorCategoria_TemPrioridade()
    {
        var db = TestSupport.NewDb();
        var svc = await ComRegrasAsync(db,
            new RegraAprovacao { Nome = "geral", ValorMinimo = 0, ValorMaximo = 100000, NivelExigido = NivelAprovacao.Gerente },
            new RegraAprovacao { Nome = "cat-7", ValorMinimo = 0, ValorMaximo = 100000, CategoriaId = 7, NivelExigido = NivelAprovacao.Diretor });

        // Categoria 7 => regra específica (Diretor) vence a geral (Gerente)
        (await svc.ResolverNivelAsync(5000m, 7, 1, 1)).Should().Be(NivelAprovacao.Diretor);
        // Outra categoria => cai na geral
        (await svc.ResolverNivelAsync(5000m, 3, 1, 1)).Should().Be(NivelAprovacao.Gerente);
    }

    [Fact]
    public async Task Resolver_SemRegra_RetornaNull()
    {
        var db = TestSupport.NewDb();
        var svc = new RegraAprovacaoService(db);
        (await svc.ResolverNivelAsync(5000m, 1, 1, 1)).Should().BeNull();
    }

    [Fact]
    public async Task Resolver_RegraInativa_Ignorada()
    {
        var db = TestSupport.NewDb();
        var svc = await ComRegrasAsync(db,
            new RegraAprovacao { Nome = "off", Ativa = false, ValorMinimo = 0, ValorMaximo = 100000, NivelExigido = NivelAprovacao.Diretor });
        (await svc.ResolverNivelAsync(5000m, 1, 1, 1)).Should().BeNull();
    }
}
