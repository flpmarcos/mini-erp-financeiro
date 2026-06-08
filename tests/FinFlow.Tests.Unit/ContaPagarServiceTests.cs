using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinFlow.Tests.Unit;

public class ContaPagarServiceTests
{
    private static (ContaPagarService svc, FinFlow.Data.AppDbContext db) Build()
    {
        var db = TestSupport.NewDb();
        var auditoria = new Mock<IAuditoriaService>();
        return (new ContaPagarService(db, auditoria.Object), db);
    }

    [Fact]
    public async Task CriarAsync_ComRetencaoIss_CalculaValorLiquido()
    {
        var (svc, db) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        var vm = new ContaPagarFormVM
        {
            Descricao = "Servico", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 1000m, DataVencimento = DateTime.Today.AddDays(30),
            Retencoes = new List<RetencaoInputVM> { new() { Tipo = TipoImposto.Iss, Aliquota = 5m } }
        };

        var r = await svc.CriarAsync(vm, "tester");

        r.Sucesso.Should().BeTrue();
        r.Dados!.ValorLiquido.Should().Be(950m);
        r.Dados.Retencoes.Should().ContainSingle(x => x.Valor == 50m);
        r.Dados.Status.Should().Be(StatusConta.Pendente);
    }

    [Fact]
    public async Task CriarAsync_ValorZero_Falha()
    {
        var (svc, db) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        var vm = new ContaPagarFormVM
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 0m, DataVencimento = DateTime.Today
        };

        var r = await svc.CriarAsync(vm, "tester");
        r.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task GerarParcelamento_DivideValor_EVinculaParcelas()
    {
        var (svc, db) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        var r = await svc.GerarParcelamentoAsync(new ParcelamentoVM
        {
            Descricao = "Compra", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorTotal = 6000m, Parcelas = 6, PrimeiroVencimento = DateTime.Today.AddDays(30)
        }, "tester");

        r.Sucesso.Should().BeTrue();
        var parcelas = await svc.ListarParcelasAsync(r.Dados!.Id);

        parcelas.Should().HaveCount(6);
        parcelas.Sum(p => p.ValorOriginal).Should().Be(6000m);
        parcelas.Should().OnlyContain(p => p.ContaOrigemId == r.Dados.Id);
        parcelas.Select(p => p.NumeroParcela).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6 });
    }

    [Fact]
    public async Task GerarParcelamento_UltimaParcelaAbsorveResto()
    {
        var (svc, db) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);

        // 100 / 3 = 33,33 ; ultima = 33,34 (soma exata 100)
        var r = await svc.GerarParcelamentoAsync(new ParcelamentoVM
        {
            Descricao = "Compra", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorTotal = 100m, Parcelas = 3, PrimeiroVencimento = DateTime.Today
        }, "tester");

        var parcelas = (await svc.ListarParcelasAsync(r.Dados!.Id)).OrderBy(p => p.NumeroParcela).ToList();
        parcelas.Sum(p => p.ValorOriginal).Should().Be(100m);
        parcelas[0].ValorOriginal.Should().Be(33.33m);
        parcelas[2].ValorOriginal.Should().Be(33.34m);
    }

    [Fact]
    public async Task CancelarAsync_ContaPaga_NaoPodeCancelar()
    {
        var (svc, db) = Build();
        var (fid, cid, ccid) = await TestSupport.SeedCadastrosAsync(db);
        var conta = new FinFlow.Domain.Entities.ContaPagar
        {
            Descricao = "x", FornecedorId = fid, CategoriaId = cid, CentroCustoId = ccid,
            ValorOriginal = 100m, ValorLiquido = 100m, DataVencimento = DateTime.Today,
            Status = StatusConta.Paga
        };
        db.ContasPagar.Add(conta);
        await db.SaveChangesAsync();

        var r = await svc.CancelarAsync(conta.Id, "tester");
        r.Sucesso.Should().BeFalse();
    }
}
