using FinFlow.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinFlow.Infrastructure.Jobs;

/// <summary>
/// Jobs financeiros executados em background (Hangfire). Reutilizam os Services.
/// Agendados como recorrentes no Program.cs e disparáveis manualmente no /hangfire.
/// </summary>
public class JobsFinanceiros
{
    private readonly IContaPagarService _pagar;
    private readonly IContaReceberService _receber;
    private readonly IBankIntegrationService _bank;
    private readonly ILogger<JobsFinanceiros> _logger;

    public JobsFinanceiros(IContaPagarService pagar, IContaReceberService receber,
        IBankIntegrationService bank, ILogger<JobsFinanceiros> logger)
    {
        _pagar = pagar;
        _receber = receber;
        _bank = bank;
        _logger = logger;
    }

    /// <summary>Marca como Vencidas as contas (a pagar e a receber) que passaram do vencimento.</summary>
    public async Task AtualizarVencidasAsync()
    {
        var ap = await _pagar.AtualizarVencidasAsync();
        var ar = await _receber.AtualizarVencidasAsync();
        _logger.LogInformation("Job vencidas: {AP} a pagar e {AR} a receber marcadas como vencidas.", ap, ar);
    }

    /// <summary>Envia alertas (simulados) das contas que vencem em breve.</summary>
    public Task EnviarAlertasVencimentoAsync()
    {
        // Aqui entraria o envio real (e-mail/WhatsApp). Por ora apenas loga (fake).
        _logger.LogInformation("Job alertas: verificando contas a vencer e notificando responsaveis (fake).");
        return Task.CompletedTask;
    }

    /// <summary>Consulta status / reprocessa transações bancárias pendentes (retry).</summary>
    public async Task ReprocessarTransacoesPendentesAsync()
    {
        var confirmadas = await _bank.ReprocessarPendentesAsync("job");
        _logger.LogInformation("Job retry bancario: {N} transacoes pendentes confirmadas.", confirmadas);
    }
}
