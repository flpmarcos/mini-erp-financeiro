using Serilog.Context;

namespace FinFlow.Infrastructure.Observability;

/// <summary>
/// Garante um CorrelationId por request (header X-Correlation-ID) e o injeta
/// no contexto do Serilog, de modo que todo log do request carregue o mesmo id.
/// Facilita rastrear um fluxo ponta-a-ponta nos logs.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
