using FinFlow.Domain.Enums;
using FinFlow.Domain.Identity;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Controllers;

/// <summary>Chat interno entre áreas (Módulo 24). UI + endpoints JSON; envio em tempo real via SignalR (/chatHub).</summary>
public class ChatController : BaseController
{
    private readonly IChatService _chat;
    private readonly UserManager<AppUser> _users;

    public ChatController(IChatService chat, UserManager<AppUser> users)
    {
        _chat = chat;
        _users = users;
    }

    private bool EhAuditor => User.IsInRole(Roles.Auditor);

    private AreaEmpresa AreaDoUsuario() =>
        User.IsInRole(Roles.Auditor) ? AreaEmpresa.Auditoria
        : User.IsInRole(Roles.Financeiro) ? AreaEmpresa.Financeiro
        : User.IsInRole(Roles.Diretor) ? AreaEmpresa.Diretoria
        : AreaEmpresa.Operacoes;

    public IActionResult Index(int? conversa)
    {
        ViewBag.ConversaInicial = conversa;
        ViewBag.Areas = Enum.GetNames<AreaEmpresa>();
        ViewBag.Usuarios = _users.Users.Select(u => u.Email).Where(e => e != null && e != UsuarioAtual).ToList();
        ViewBag.MinhaArea = AreaDoUsuario().ToString();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Conversas()
    {
        var lista = await _chat.ListarConversasAsync(UsuarioAtual, EhAuditor);
        var dtos = lista.Select(c => new ChatConversationDto(
            c.Id, c.Titulo ?? "Conversa", c.Tipo.ToString(), c.Area?.ToString(), c.EhAuditavel,
            c.ContaPagarId, c.FornecedorId, c.CriadoEm));
        return Json(dtos);
    }

    [HttpGet]
    public async Task<IActionResult> Historico(int id)
    {
        var msgs = await _chat.HistoricoAsync(id, UsuarioAtual, EhAuditor);
        var dtos = msgs.Select(m => new ChatMessageDto(m.Id, m.ConversationId, m.Autor, m.AutorArea.ToString(),
            m.Texto, m.Fixada, m.Excluida, m.CriadoEm, m.Mencoes.Select(x => x.UsuarioMencionado)));
        return Json(dtos);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(NovaConversaVM vm)
    {
        var r = await _chat.CriarConversaAsync(vm, UsuarioAtual, AreaDoUsuario());
        if (!r.Sucesso) return BadRequest(r.Erro);
        return Json(new { id = r.Dados!.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLida(int messageId)
    {
        await _chat.MarcarLidaAsync(messageId, UsuarioAtual);
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Fixar(int messageId)
    {
        var r = await _chat.FixarAsync(messageId, UsuarioAtual, EhAuditor);
        return r.Sucesso ? Ok() : BadRequest(r.Erro);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int messageId)
    {
        var r = await _chat.ExcluirMensagemAsync(messageId, UsuarioAtual);
        return r.Sucesso ? Ok() : BadRequest(r.Erro);
    }
}
