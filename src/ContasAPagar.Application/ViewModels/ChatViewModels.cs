using System.ComponentModel.DataAnnotations;
using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.ViewModels;

public class NovaConversaVM
{
    [StringLength(160)]
    public string? Titulo { get; set; }
    public TipoConversa Tipo { get; set; } = TipoConversa.Grupo;
    public AreaEmpresa? Area { get; set; }

    public int? ContaPagarId { get; set; }
    public int? FornecedorId { get; set; }
    public int? SolicitacaoCompraId { get; set; }

    /// <summary>Logins dos participantes (o criador é incluído automaticamente).</summary>
    public List<string> Participantes { get; set; } = new();
}

/// <summary>Mensagem serializada para API/SignalR.</summary>
public record ChatMessageDto(
    int Id, int ConversationId, string Autor, string AutorArea, string Texto,
    bool Fixada, bool Excluida, DateTime CriadoEm, IEnumerable<string> Mencoes);

public record ChatConversationDto(
    int Id, string Titulo, string Tipo, string? Area, bool Auditavel,
    int? ContaPagarId, int? FornecedorId, DateTime CriadoEm);
