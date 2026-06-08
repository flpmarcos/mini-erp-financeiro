using System.Text;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinFlow.Tests.Unit;

public class ConciliacaoServiceTests
{
    private static (ConciliacaoService svc, FinFlow.Data.AppDbContext db) Build()
    {
        var db = TestSupport.NewDb();
        return (new ConciliacaoService(db, new Mock<IAuditoriaService>().Object), db);
    }

    private static Stream Csv(string corpo) => new MemoryStream(Encoding.UTF8.GetBytes(corpo));

    [Fact]
    public async Task Importar_ConciliaAutomaticamente_ContaAReceber()
    {
        var (svc, db) = Build();
        var cli = new Cliente { RazaoSocial = "Cliente X", Documento = "11222333000181" };
        db.Clientes.Add(cli);
        await db.SaveChangesAsync();
        db.ContasReceber.Add(new ContaReceber
        {
            Descricao = "Fatura", ClienteId = cli.Id, Valor = 1500m, ValorRecebido = 1500m,
            Status = StatusReceber.Recebida, DataRecebimento = DateTime.Today, DataVencimento = DateTime.Today
        });
        await db.SaveChangesAsync();

        var csv = $"Data;Descricao;Valor;Documento;Banco;Tipo\n{DateTime.Today:dd/MM/yyyy};Recebimento cliente;1500,00;DOC1;BB;PIX\n";
        var r = await svc.ImportarCsvAsync(Csv(csv), "tester");

        r.Sucesso.Should().BeTrue();
        r.Dados!.ConciliadosAutomaticamente.Should().Be(1);
        var item = await db.ExtratoItens.SingleAsync();
        item.ContaReceberId.Should().NotBeNull();
        item.Status.Should().Be(StatusConciliacao.Conciliado);
    }

    [Fact]
    public async Task ConciliarReceberManual_VinculaContaAReceber()
    {
        var (svc, db) = Build();
        var cli = new Cliente { RazaoSocial = "Y", Documento = "11222333000181" };
        db.Clientes.Add(cli);
        await db.SaveChangesAsync();
        var conta = new ContaReceber { Descricao = "F", ClienteId = cli.Id, Valor = 100m, ValorRecebido = 100m, Status = StatusReceber.Recebida, DataVencimento = DateTime.Today };
        db.ContasReceber.Add(conta);
        db.ExtratoItens.Add(new ExtratoBancarioItem { Data = DateTime.Today, Descricao = "x", Valor = 100m });
        await db.SaveChangesAsync();
        var item = await db.ExtratoItens.SingleAsync();

        var r = await svc.ConciliarReceberManualAsync(item.Id, conta.Id, "tester");

        r.Sucesso.Should().BeTrue();
        (await db.ExtratoItens.FindAsync(item.Id))!.ContaReceberId.Should().Be(conta.Id);
    }
}
