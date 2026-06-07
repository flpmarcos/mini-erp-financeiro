using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Integrations.Banking;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AccountsPayable.Tests.Unit;

public class BankPaymentFactoryTests
{
    private static BankPaymentServiceFactory Factory() => new(new IBankPaymentService[]
    {
        new GenericoPaymentServiceFake(NullLogger<GenericoPaymentServiceFake>.Instance),
        new BancoBrasilPaymentServiceFake(NullLogger<BancoBrasilPaymentServiceFake>.Instance),
        new ItauPaymentServiceFake(NullLogger<ItauPaymentServiceFake>.Instance),
        new SantanderPaymentServiceFake(NullLogger<SantanderPaymentServiceFake>.Instance),
    });

    [Theory]
    [InlineData(BancoIntegracao.BancoDoBrasil)]
    [InlineData(BancoIntegracao.Itau)]
    [InlineData(BancoIntegracao.Santander)]
    [InlineData(BancoIntegracao.Generico)]
    public void Resolver_RetornaAdapterDoBancoCorreto(BancoIntegracao banco) =>
        Factory().Resolver(banco).Banco.Should().Be(banco);

    [Fact]
    public async Task Fake_PagarAsync_RegistraPayloadsDeEnvioEResposta()
    {
        var svc = new ItauPaymentServiceFake(NullLogger<ItauPaymentServiceFake>.Instance);
        var resp = await svc.PagarAsync(new BankPaymentRequest
        {
            ContaPagarId = 1, Valor = 100m, Forma = FormaPagamento.Pix, Favorecido = "ACME"
        });

        resp.PayloadEnvio.Should().NotBeNullOrEmpty();
        resp.PayloadResposta.Should().NotBeNullOrEmpty();
        resp.Status.Should().BeOneOf(
            StatusTransacaoBancaria.Sucesso,
            StatusTransacaoBancaria.Pendente,
            StatusTransacaoBancaria.Erro);
    }
}
