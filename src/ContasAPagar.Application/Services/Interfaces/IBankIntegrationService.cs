using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;

namespace ContasAPagar.Web.Services.Interfaces;

public interface IBankIntegrationService
{
    /// <summary>Estorna uma conta paga: reverte o pagamento e registra transação de estorno.</summary>
    Task<OperationResult> EstornarAsync(int contaPagarId, string motivo, string usuario);

    /// <summary>Processa um evento de webhook do banco (confirma/recusa/estorna a transação).</summary>
    Task<OperationResult> ProcessarWebhookAsync(string codigoTransacao, StatusTransacaoBancaria status, string? payload, string usuario);

    /// <summary>Reprocessa transações pendentes (retry/consulta de status). Retorna quantas foram confirmadas.</summary>
    Task<int> ReprocessarPendentesAsync(string usuario);
}
