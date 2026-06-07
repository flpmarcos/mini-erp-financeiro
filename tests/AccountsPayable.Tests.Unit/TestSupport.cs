using ContasAPagar.Web.Configurations;
using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AccountsPayable.Tests.Unit;

/// <summary>Helpers compartilhados: AppDbContext InMemory isolado por teste + cadastros base.</summary>
public static class TestSupport
{
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options);
    }

    public static IOptions<FinanceiroOptions> Financeiro(
        decimal multa = 2.0m, decimal jurosDia = 0.033m,
        decimal limAuto = 1000m, decimal limGerente = 10000m) =>
        Options.Create(new FinanceiroOptions
        {
            MultaPercentual = multa,
            JurosDiarioPercentual = jurosDia,
            LimiteAprovacaoAutomatica = limAuto,
            LimiteAprovacaoGerente = limGerente
        });

    /// <summary>Cria fornecedor + categoria + centro e devolve os ids.</summary>
    public static async Task<(int fornecedorId, int categoriaId, int centroId)> SeedCadastrosAsync(AppDbContext db)
    {
        var f = new Fornecedor { RazaoSocial = "Fornecedor Teste", Documento = "11222333000181" };
        var c = new Categoria { Nome = "Servicos" };
        var cc = new CentroCusto { Codigo = "TI", Nome = "Tecnologia" };
        db.AddRange(f, c, cc);
        await db.SaveChangesAsync();
        return (f.Id, c.Id, cc.Id);
    }
}
