using FinFlow.Data;
using FinFlow.Domain.Enums;
using FinFlow.Domain.Identity;
using FinFlow.Infrastructure.Tenancy;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FinFlow.Hubs;

/// <summary>Hub SignalR do chat interno: entra na conversa, envia mensagem em tempo real, indica digitação.</summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chat;
    private readonly AppDbContext _db;

    public ChatHub(IChatService chat, AppDbContext db)
    {
        _chat = chat;
        _db = db;
    }

    private string Usuario => Context.User?.Identity?.Name ?? "anon";
    private bool EhAuditor => Context.User?.IsInRole(Roles.Auditor) ?? false;
    private static string Grupo(int conversationId) => $"conv-{conversationId}";

    /// <summary>
    /// O TenantMiddleware não roda em invocações de hub (só no request HTTP). Aqui aplicamos
    /// o tenant (empresa) no DbContext da chamada a partir do claim do usuário, garantindo
    /// o isolamento multiempresa também no chat em tempo real.
    /// </summary>
    private void AplicarTenant()
    {
        if (int.TryParse(Context.User?.FindFirst(TenantClaims.EmpresaId)?.Value, out var empresaId))
            _db.EmpresaIdFiltro = empresaId;
    }

    public Task Entrar(int conversationId) => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(conversationId));
    public Task Sair(int conversationId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(conversationId));

    public async Task Enviar(int conversationId, string texto)
    {
        AplicarTenant();
        var r = await _chat.EnviarMensagemAsync(conversationId, Usuario, AreaEmpresa.Operacoes, texto, EhAuditor);
        if (!r.Sucesso)
        {
            await Clients.Caller.SendAsync("Erro", r.Erro);
            return;
        }
        var m = r.Dados!;
        var dto = new ChatMessageDto(m.Id, m.ConversationId, m.Autor, m.AutorArea.ToString(), m.Texto,
            m.Fixada, m.Excluida, m.CriadoEm, m.Mencoes.Select(x => x.UsuarioMencionado));
        await Clients.Group(Grupo(conversationId)).SendAsync("ReceberMensagem", dto);
    }

    public Task Digitando(int conversationId) =>
        Clients.OthersInGroup(Grupo(conversationId)).SendAsync("Digitando", Usuario);
}
