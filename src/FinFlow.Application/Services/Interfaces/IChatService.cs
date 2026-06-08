using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

public interface IChatService
{
    Task<List<ChatConversation>> ListarConversasAsync(string usuario, bool ehAuditor);
    Task<ChatConversation?> ObterAsync(int conversationId, string usuario, bool ehAuditor);
    Task<bool> ParticipaAsync(int conversationId, string usuario, bool ehAuditor);

    Task<OperationResult<ChatConversation>> CriarConversaAsync(NovaConversaVM vm, string criadoPor, AreaEmpresa areaCriador);

    Task<OperationResult<ChatMessage>> EnviarMensagemAsync(int conversationId, string autor, AreaEmpresa area, string texto, bool ehAuditor);
    Task<List<ChatMessage>> HistoricoAsync(int conversationId, string usuario, bool ehAuditor, int take = 100);
    Task<List<ChatMessage>> BuscarAsync(int conversationId, string termo, string usuario, bool ehAuditor);

    Task<OperationResult> MarcarLidaAsync(int messageId, string usuario);
    Task<OperationResult> FixarAsync(int messageId, string usuario, bool ehAuditor);
    Task<OperationResult> ExcluirMensagemAsync(int messageId, string usuario);
}
