using System.Text.Json;
using FinFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinFlow.Integrations.Banking;

/// <summary>
/// Base dos servicos bancarios FAKE. Simula PIX/TED/Boleto e retornos
/// de sucesso, erro e pendente. Loga payloads de envio e resposta.
/// Nao chama nenhuma API real - apenas estrutura o fluxo para troca futura.
/// </summary>
public abstract class FakeBankPaymentService : IBankPaymentService
{
    private readonly ILogger _logger;
    // Random.Shared é thread-safe — evita corrupção do gerador em pagamentos concorrentes.
    private static readonly Random _rnd = Random.Shared;

    protected FakeBankPaymentService(ILogger logger) => _logger = logger;

    public abstract BancoIntegracao Banco { get; }

    public Task<BankPaymentResponse> PagarAsync(BankPaymentRequest request)
    {
        var payloadEnvio = JsonSerializer.Serialize(new
        {
            banco = Banco.ToString(),
            request.ContaPagarId,
            request.Valor,
            forma = request.Forma.ToString(),
            request.Favorecido,
            request.ChavePix,
            request.CodigoBarras
        });

        _logger.LogInformation("[{Banco}] Enviando pagamento conta {Id} valor {Valor}",
            Banco, request.ContaPagarId, request.Valor);

        // Simula resultado: ~80% sucesso, ~10% pendente, ~10% erro.
        var sorteio = _rnd.Next(0, 100);
        BankPaymentResponse resp = sorteio switch
        {
            < 80 => Sucesso(request, payloadEnvio),
            < 90 => Pendente(request, payloadEnvio),
            _ => Erro(request, payloadEnvio)
        };

        _logger.LogInformation("[{Banco}] Resposta conta {Id}: {Status} {Codigo}",
            Banco, request.ContaPagarId, resp.Status, resp.CodigoTransacao);

        return Task.FromResult(resp);
    }

    private BankPaymentResponse Sucesso(BankPaymentRequest req, string envio)
    {
        var codigo = $"{Prefixo()}{_rnd.Next(100000, 999999)}";
        var respPayload = JsonSerializer.Serialize(new { status = "CONFIRMADO", e2e = codigo, data = DateTime.UtcNow });
        return new BankPaymentResponse
        {
            Status = StatusTransacaoBancaria.Sucesso,
            CodigoTransacao = codigo,
            PayloadEnvio = envio,
            PayloadResposta = respPayload
        };
    }

    private BankPaymentResponse Pendente(BankPaymentRequest req, string envio)
    {
        var codigo = $"{Prefixo()}{_rnd.Next(100000, 999999)}";
        var respPayload = JsonSerializer.Serialize(new { status = "EM_PROCESSAMENTO", protocolo = codigo });
        return new BankPaymentResponse
        {
            Status = StatusTransacaoBancaria.Pendente,
            CodigoTransacao = codigo,
            PayloadEnvio = envio,
            PayloadResposta = respPayload
        };
    }

    private BankPaymentResponse Erro(BankPaymentRequest req, string envio)
    {
        var respPayload = JsonSerializer.Serialize(new { status = "REJEITADO", erro = "Saldo insuficiente ou dados invalidos" });
        return new BankPaymentResponse
        {
            Status = StatusTransacaoBancaria.Erro,
            MensagemErro = "Pagamento rejeitado pelo banco (simulado)",
            PayloadEnvio = envio,
            PayloadResposta = respPayload
        };
    }

    /// <summary>Prefixo do codigo de transacao por banco (ajuda a identificar a origem).</summary>
    protected abstract string Prefixo();
}

public sealed class GenericoPaymentServiceFake(ILogger<GenericoPaymentServiceFake> logger)
    : FakeBankPaymentService(logger)
{
    public override BancoIntegracao Banco => BancoIntegracao.Generico;
    protected override string Prefixo() => "GEN";
}

public sealed class BancoBrasilPaymentServiceFake(ILogger<BancoBrasilPaymentServiceFake> logger)
    : FakeBankPaymentService(logger)
{
    public override BancoIntegracao Banco => BancoIntegracao.BancoDoBrasil;
    protected override string Prefixo() => "BB";
}

public sealed class ItauPaymentServiceFake(ILogger<ItauPaymentServiceFake> logger)
    : FakeBankPaymentService(logger)
{
    public override BancoIntegracao Banco => BancoIntegracao.Itau;
    protected override string Prefixo() => "ITAU";
}

public sealed class SantanderPaymentServiceFake(ILogger<SantanderPaymentServiceFake> logger)
    : FakeBankPaymentService(logger)
{
    public override BancoIntegracao Banco => BancoIntegracao.Santander;
    protected override string Prefixo() => "SANT";
}
