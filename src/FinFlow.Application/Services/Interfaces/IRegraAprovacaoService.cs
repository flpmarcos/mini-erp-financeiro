using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

public interface IRegraAprovacaoService
{
    Task<List<RegraAprovacao>> ListarAsync();
    Task<RegraAprovacao?> ObterAsync(int id);
    Task<OperationResult<RegraAprovacao>> CriarAsync(RegraAprovacao r);
    Task<OperationResult> AtualizarAsync(RegraAprovacao r);
    Task<OperationResult> RemoverAsync(int id);

    /// <summary>
    /// Resolve a alçada exigida para uma conta segundo as regras configuradas.
    /// Escolhe a regra ativa mais específica que casa. Sem regra → null (usar fallback por valor).
    /// </summary>
    Task<NivelAprovacao?> ResolverNivelAsync(decimal valor, int categoriaId, int centroCustoId, int fornecedorId);
}
