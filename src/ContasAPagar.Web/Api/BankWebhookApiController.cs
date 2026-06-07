using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContasAPagar.Web.Api;

public record WebhookRequest(string CodigoTransacao, StatusTransacaoBancaria Status, string? Payload);

/// <summary>
/// Recebe webhooks do banco (confirmação assíncrona de pagamento).
/// Anônimo, protegido por token simples no header X-Webhook-Token (config Bank:WebhookToken).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/bank/webhook")]
public class BankWebhookApiController : ControllerBase
{
    private readonly IBankIntegrationService _bank;
    private readonly IConfiguration _config;

    public BankWebhookApiController(IBankIntegrationService bank, IConfiguration config)
    {
        _bank = bank;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] WebhookRequest req, [FromHeader(Name = "X-Webhook-Token")] string? token)
    {
        var esperado = _config["Bank:WebhookToken"] ?? "dev-token";
        if (token != esperado) return Unauthorized(new ApiResponse<string>(false, Error: "Token de webhook invalido."));

        if (string.IsNullOrWhiteSpace(req.CodigoTransacao))
            return BadRequest(new ApiResponse<string>(false, Error: "CodigoTransacao obrigatorio."));

        var r = await _bank.ProcessarWebhookAsync(req.CodigoTransacao, req.Status, req.Payload, "webhook");
        return r.Sucesso
            ? Ok(new ApiResponse<string>(true, "Processado."))
            : NotFound(new ApiResponse<string>(false, Error: r.Erro));
    }
}
