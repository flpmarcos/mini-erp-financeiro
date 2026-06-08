using FinFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FinFlow.Integrations.Notifications;

/// <summary>Canal de saída de notificação. Hoje fakes (log); troque por SMTP/WhatsApp real.</summary>
public interface INotificationChannel
{
    string Nome { get; }
    Task EnviarAsync(Notificacao notificacao);
}

public sealed class FakeEmailChannel(ILogger<FakeEmailChannel> logger) : INotificationChannel
{
    public string Nome => "email";
    public Task EnviarAsync(Notificacao n)
    {
        logger.LogInformation("[EMAIL fake] para {Dest}: {Titulo}", n.Destinatario, n.Titulo);
        return Task.CompletedTask;
    }
}

public sealed class FakeWhatsappChannel(ILogger<FakeWhatsappChannel> logger) : INotificationChannel
{
    public string Nome => "whatsapp";
    public Task EnviarAsync(Notificacao n)
    {
        logger.LogInformation("[WHATSAPP fake] para {Dest}: {Titulo}", n.Destinatario, n.Titulo);
        return Task.CompletedTask;
    }
}
