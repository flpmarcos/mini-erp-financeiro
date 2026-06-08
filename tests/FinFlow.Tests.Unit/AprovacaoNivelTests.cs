using FinFlow.Domain.Enums;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FinFlow.Tests.Unit;

public class AprovacaoNivelTests
{
    private static AprovacaoService Svc()
    {
        var db = TestSupport.NewDb();
        var auditoria = new Mock<IAuditoriaService>();
        var regras = new Mock<IRegraAprovacaoService>();
        var notif = new Mock<INotificacaoService>();
        return new AprovacaoService(db, auditoria.Object, regras.Object, notif.Object, TestSupport.Financeiro());
    }

    [Theory]
    [InlineData(999.99, NivelAprovacao.Automatica)]   // < 1000
    [InlineData(1000, NivelAprovacao.Gerente)]        // limite inferior gerente
    [InlineData(5000, NivelAprovacao.Gerente)]
    [InlineData(10000, NivelAprovacao.Gerente)]       // limite superior gerente (inclusive)
    [InlineData(10000.01, NivelAprovacao.Diretor)]    // > 10000
    [InlineData(50000, NivelAprovacao.Diretor)]
    public void DeterminarNivel_RespeitaAlcadas(decimal valor, NivelAprovacao esperado) =>
        Svc().DeterminarNivel(valor).Should().Be(esperado);
}
