using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Domain.Identity;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ContasAPagar.Web.Hubs;

/// <summary>Hub SignalR do chat interno: entra na conversa, envia mensagem em tempo real, indica digitação.</summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chat;
    public ChatHub(IChatService chat) => _chat = chat;

    private string Usuario => Context.User?.Identity?.Name ?? "anon";
    private bool EhAuditor => Context.User?.IsInRole(Roles.Auditor) ?? false;
    private static string Grupo(int conversationId) => $"conv-{conversationId}";

    public Task Entrar(int conversationId) => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(conversationId));
    public Task Sair(int conversationId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(conversationId));

    public async Task Enviar(int conversationId, string texto)
    {
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
