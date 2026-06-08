using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinFlow.Tests.Unit;

public class ContabilidadeServiceTests
{
    private static async Task<(ContabilidadeService svc, FinFlow.Data.AppDbContext db, int bancos, int despesas, int receitas)> BuildAsync()
    {
        var db = TestSupport.NewDb();
        var bancos = new ContaContabil { Codigo = "1.1.01", Nome = "Bancos", Tipo = TipoContaContabil.Ativo, Natureza = NaturezaConta.Devedora };
        var despesas = new ContaContabil { Codigo = "4.1.01", Nome = "Despesas", Tipo = TipoContaContabil.Despesa, Natureza = NaturezaConta.Devedora };
        var receitas = new ContaContabil { Codigo = "3.1.01", Nome = "Receitas", Tipo = TipoContaContabil.Receita, Natureza = NaturezaConta.Credora };
        db.ContasContabeis.AddRange(bancos, despesas, receitas);
        await db.SaveChangesAsync();
        var svc = new ContabilidadeService(db, new Mock<IAuditoriaService>().Object);
        return (svc, db, bancos.Id, despesas.Id, receitas.Id);
    }

    [Fact]
    public async Task CriarLancamento_Balanceado_Persiste()
    {
        var (svc, _, bancos, despesas, _) = await BuildAsync();
        var vm = new LancamentoFormVM
        {
            Historico = "Pagto aluguel",
            Partidas = new()
            {
                new PartidaInputVM { ContaContabilId = despesas, Tipo = TipoPartida.Debito, Valor = 100m },
                new PartidaInputVM { ContaContabilId = bancos, Tipo = TipoPartida.Credito, Valor = 100m },
            }
        };
        (await svc.CriarLancamentoAsync(vm, "t")).Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task CriarLancamento_Desbalanceado_Falha()
    {
        var (svc, _, bancos, despesas, _) = await BuildAsync();
        var vm = new LancamentoFormVM
        {
            Historico = "x",
            Partidas = new()
            {
                new PartidaInputVM { ContaContabilId = despesas, Tipo = TipoPartida.Debito, Valor = 100m },
                new PartidaInputVM { ContaContabilId = bancos, Tipo = TipoPartida.Credito, Valor = 50m },
            }
        };
        (await svc.CriarLancamentoAsync(vm, "t")).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task LancarPagamento_GeraPartidaDobrada_DDespesaCBancos()
    {
        var (svc, db, bancos, despesas, _) = await BuildAsync();

        await svc.LancarPagamentoAsync(contaPagarId: 42, valor: 500m, usuario: "fin");

        var lanc = await db.Lancamentos.Include(l => l.Partidas).SingleAsync();
        lanc.Balanceado.Should().BeTrue();
        lanc.Partidas.Single(p => p.Tipo == TipoPartida.Debito).ContaContabilId.Should().Be(despesas);
        lanc.Partidas.Single(p => p.Tipo == TipoPartida.Credito).ContaContabilId.Should().Be(bancos);
    }

    [Fact]
    public async Task EstornarPagamento_GeraReversao_e_ZeraSaldo()
    {
        var (svc, db, _, _, _) = await BuildAsync();
        await svc.LancarPagamentoAsync(contaPagarId: 42, valor: 500m, usuario: "fin");

        await svc.EstornarPagamentoAsync(contaPagarId: 42, usuario: "fin");

        (await db.Lancamentos.CountAsync()).Should().Be(2); // original + estorno
        var dre = await svc.DreAsync();
        dre.Despesas.Should().Be(0m); // pagamento revertido
    }

    [Fact]
    public async Task EstornarPagamento_Idempotente_NaoDuplicaReversao()
    {
        var (svc, db, _, _, _) = await BuildAsync();
        await svc.LancarPagamentoAsync(contaPagarId: 42, valor: 500m, usuario: "fin");

        await svc.EstornarPagamentoAsync(42, "fin");
        await svc.EstornarPagamentoAsync(42, "fin"); // 2a vez = no-op

        (await db.Lancamentos.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Recebimento_ApareceNaDRE_e_BalanceteBate()
    {
        var (svc, _, _, _, _) = await BuildAsync();
        await svc.LancarRecebimentoAsync(contaReceberId: 7, valor: 1000m, usuario: "fin");

        var dre = await svc.DreAsync();
        dre.Receitas.Should().Be(1000m);
        dre.Resultado.Should().Be(1000m);

        var bal = await svc.BalanceteAsync();
        bal.Sum(l => l.Debito).Should().Be(bal.Sum(l => l.Credito)); // partida dobrada fecha
    }
}
