using System.Text.RegularExpressions;
using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Chat interno entre áreas (Módulo 24). Regras: usuário só vê conversas em que
/// participa; auditor vê conversas auditáveis; mensagens vinculadas a pagamento
/// nunca são apagadas fisicamente (soft delete). Menções (@) notificam usuários.
/// </summary>
public class ChatService : IChatService
{
    private static readonly Regex MentionRegex = new(@"@([A-Za-z0-9._@-]+)", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly INotificacaoService _notificacoes;
    private readonly IAuditoriaService _auditoria;

    public ChatService(AppDbContext db, INotificacaoService notificacoes, IAuditoriaService auditoria)
    {
        _db = db;
        _notificacoes = notificacoes;
        _auditoria = auditoria;
    }

    public async Task<List<ChatConversation>> ListarConversasAsync(string usuario, bool ehAuditor)
    {
        var idsParticipa = await _db.ChatParticipantes
            .Where(p => p.Usuario == usuario).Select(p => p.ConversationId).ToListAsync();

        var query = _db.Conversas.AsNoTracking()
            .Include(c => c.Participantes)
            .Where(c => idsParticipa.Contains(c.Id)
                     || (ehAuditor && (c.ContaPagarId != null || c.SolicitacaoCompraId != null || c.FornecedorId != null)));

        return await query.OrderByDescending(c => c.AtualizadoEm ?? c.CriadoEm).ToListAsync();
    }

    public Task<ChatConversation?> ObterAsync(int conversationId, string usuario, bool ehAuditor) =>
        _db.Conversas.Include(c => c.Participantes)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

    public async Task<bool> ParticipaAsync(int conversationId, string usuario, bool ehAuditor)
    {
        if (await _db.ChatParticipantes.AnyAsync(p => p.ConversationId == conversationId && p.Usuario == usuario))
            return true;
        if (ehAuditor)
            return await _db.Conversas.AnyAsync(c => c.Id == conversationId
                && (c.ContaPagarId != null || c.SolicitacaoCompraId != null || c.FornecedorId != null));
        return false;
    }

    public async Task<OperationResult<ChatConversation>> CriarConversaAsync(NovaConversaVM vm, string criadoPor, AreaEmpresa areaCriador)
    {
        var conversa = new ChatConversation
        {
            Titulo = vm.Titulo ?? (vm.Tipo == TipoConversa.Area ? $"Área {vm.Area}" : "Nova conversa"),
            Tipo = vm.Tipo,
            Area = vm.Area,
            ContaPagarId = vm.ContaPagarId,
            FornecedorId = vm.FornecedorId,
            SolicitacaoCompraId = vm.SolicitacaoCompraId,
            CriadoPor = criadoPor
        };
        conversa.Participantes.Add(new ChatParticipant { Usuario = criadoPor, Area = areaCriador });
        foreach (var u in vm.Participantes.Where(u => !string.IsNullOrWhiteSpace(u) && u != criadoPor).Distinct())
            conversa.Participantes.Add(new ChatParticipant { Usuario = u, Area = vm.Area ?? AreaEmpresa.Operacoes });

        _db.Conversas.Add(conversa);
        await _db.SaveChangesAsync();
        return OperationResult<ChatConversation>.Ok(conversa);
    }

    public async Task<OperationResult<ChatMessage>> EnviarMensagemAsync(int conversationId, string autor, AreaEmpresa area, string texto, bool ehAuditor)
    {
        if (string.IsNullOrWhiteSpace(texto)) return OperationResult<ChatMessage>.Falha("Mensagem vazia.");
        if (!await ParticipaAsync(conversationId, autor, ehAuditor))
            return OperationResult<ChatMessage>.Falha("Voce nao participa desta conversa.");

        var conversa = await _db.Conversas.FirstOrDefaultAsync(c => c.Id == conversationId);
        if (conversa is null) return OperationResult<ChatMessage>.Falha("Conversa nao encontrada.");

        var participante = await _db.ChatParticipantes
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.Usuario == autor);

        var msg = new ChatMessage
        {
            ConversationId = conversationId,
            Autor = autor,
            AutorArea = participante?.Area ?? area,
            Texto = texto.Trim(),
            VinculadaPagamento = conversa.ContaPagarId.HasValue
        };

        // Menções @usuario → registra + notifica.
        foreach (Match m in MentionRegex.Matches(texto))
        {
            var mencionado = m.Groups[1].Value;
            msg.Mencoes.Add(new ChatMention { UsuarioMencionado = mencionado });
        }

        _db.ChatMensagens.Add(msg);
        conversa.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        foreach (var mencao in msg.Mencoes)
            await _notificacoes.NotificarAsync(mencao.UsuarioMencionado,
                $"Você foi mencionado por {autor}", texto.Length > 120 ? texto[..120] : texto,
                SeveridadeNotificacao.Info, $"/Chat?conversa={conversationId}");

        if (msg.VinculadaPagamento)
            await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(ChatMessage), msg.Id,
                valorNovo: "Mensagem vinculada a pagamento", usuario: autor);
        await _db.SaveChangesAsync();

        return OperationResult<ChatMessage>.Ok(msg);
    }

    public async Task<List<ChatMessage>> HistoricoAsync(int conversationId, string usuario, bool ehAuditor, int take = 100)
    {
        if (!await ParticipaAsync(conversationId, usuario, ehAuditor)) return new();
        return await _db.ChatMensagens.AsNoTracking()
            .Include(m => m.Mencoes).Include(m => m.Anexos)
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CriadoEm).Take(take).ToListAsync();
    }

    public async Task<List<ChatMessage>> BuscarAsync(int conversationId, string termo, string usuario, bool ehAuditor)
    {
        if (!await ParticipaAsync(conversationId, usuario, ehAuditor)) return new();
        return await _db.ChatMensagens.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.Excluida && m.Texto.Contains(termo))
            .OrderByDescending(m => m.CriadoEm).Take(50).ToListAsync();
    }

    public async Task<OperationResult> MarcarLidaAsync(int messageId, string usuario)
    {
        var msg = await _db.ChatMensagens.FindAsync(messageId);
        if (msg is null) return OperationResult.Falha("Mensagem nao encontrada.");
        if (!await _db.ChatLeituras.AnyAsync(r => r.MessageId == messageId && r.Usuario == usuario))
        {
            _db.ChatLeituras.Add(new ChatReadReceipt { MessageId = messageId, Usuario = usuario });
            await _db.SaveChangesAsync();
        }
        return OperationResult.Ok();
    }

    public async Task<OperationResult> FixarAsync(int messageId, string usuario, bool ehAuditor)
    {
        var msg = await _db.ChatMensagens.FindAsync(messageId);
        if (msg is null) return OperationResult.Falha("Mensagem nao encontrada.");
        if (!await ParticipaAsync(msg.ConversationId, usuario, ehAuditor)) return OperationResult.Falha("Sem permissao.");
        msg.Fixada = !msg.Fixada;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ExcluirMensagemAsync(int messageId, string usuario)
    {
        var msg = await _db.ChatMensagens.FindAsync(messageId);
        if (msg is null) return OperationResult.Falha("Mensagem nao encontrada.");
        if (msg.Autor != usuario) return OperationResult.Falha("So o autor pode excluir a propria mensagem.");

        // Soft delete: marca como excluída mas PRESERVA o texto original (auditabilidade,
        // especialmente para mensagens vinculadas a pagamento). A UI esconde o conteúdo.
        msg.Excluida = true;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
