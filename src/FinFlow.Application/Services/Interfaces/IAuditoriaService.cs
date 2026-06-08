using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

/// <summary>Registra a trilha de auditoria das acoes importantes.</summary>
public interface IAuditoriaService
{
    /// <summary>Enfileira um registro de auditoria (persistido no proximo SaveChanges do fluxo).</summary>
    Task RegistrarAsync(AcaoAuditoria acao, string entidade, int entidadeId,
        string? campo = null, string? valorAnterior = null, string? valorNovo = null,
        string usuario = "sistema", string? motivo = null);

    /// <summary>Persiste os registros de auditoria pendentes (quando o chamador não controla SaveChanges).</summary>
    Task SalvarPendentesAsync();

    Task<PagedResult<AuditLog>> ListarAsync(string? entidade, int pagina, int tamanho);
}
