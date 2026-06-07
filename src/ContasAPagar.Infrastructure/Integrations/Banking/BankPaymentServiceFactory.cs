using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.Integrations.Banking;

/// <summary>Seleciona a implementacao bancaria correta conforme o banco da conta.</summary>
public interface IBankPaymentServiceFactory
{
    IBankPaymentService Resolver(BancoIntegracao banco);
}

public sealed class BankPaymentServiceFactory : IBankPaymentServiceFactory
{
    private readonly IEnumerable<IBankPaymentService> _services;

    public BankPaymentServiceFactory(IEnumerable<IBankPaymentService> services) => _services = services;

    public IBankPaymentService Resolver(BancoIntegracao banco) =>
        _services.FirstOrDefault(s => s.Banco == banco)
        ?? _services.First(s => s.Banco == BancoIntegracao.Generico);
}
