using ContasAPagar.Web.Infrastructure.Jobs;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AccountsPayable.Tests.Unit;

public class JobsFinanceirosTests
{
    [Fact]
    public async Task AtualizarVencidas_ChamaAmbosOsServicos()
    {
        var pagar = new Mock<IContaPagarService>();
        var receber = new Mock<IContaReceberService>();
        pagar.Setup(p => p.AtualizarVencidasAsync()).ReturnsAsync(3);
        receber.Setup(r => r.AtualizarVencidasAsync()).ReturnsAsync(2);

        var bank = new Mock<IBankIntegrationService>();
        var job = new JobsFinanceiros(pagar.Object, receber.Object, bank.Object, NullLogger<JobsFinanceiros>.Instance);
        await job.AtualizarVencidasAsync();

        pagar.Verify(p => p.AtualizarVencidasAsync(), Times.Once);
        receber.Verify(r => r.AtualizarVencidasAsync(), Times.Once);
    }
}
