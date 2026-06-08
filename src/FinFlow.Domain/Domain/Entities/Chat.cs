using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Conversa do chat interno. Pode ser vinculada a entidades do sistema.</summary>
public class ChatConversation : BaseEntity, ITenantOwned
{
    public int EmpresaId { get; set; } = 1;

    [StringLength(160)]
    public string? Titulo { get; set; }

    public TipoConversa Tipo { get; set; } = TipoConversa.Grupo;

    /// <summary>Área quando a conversa é por área.</summary>
    public AreaEmpresa? Area { get; set; }

    // Vínculos opcionais (rastreabilidade / contexto)
    public int? ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }
    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public int? SolicitacaoCompraId { get; set; }
    public SolicitacaoCompra? SolicitacaoCompra { get; set; }
    public int? AnexoId { get; set; }

    [StringLength(120)]
    public string CriadoPor { get; set; } = "sistema";

    public ICollection<ChatParticipant> Participantes { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Mensagens { get; set; } = new List<ChatMessage>();

    /// <summary>Conversa vinculada a processo auditável (pagamento/compra/fornecedor).</summary>
    public bool EhAuditavel => ContaPagarId.HasValue || SolicitacaoCompraId.HasValue || FornecedorId.HasValue;
}

public class ChatParticipant : BaseEntity
{
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    [Required, StringLength(120)]
    public string Usuario { get; set; } = string.Empty;

    public AreaEmpresa Area { get; set; }
}

public class ChatMessage : BaseEntity
{
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    [Required, StringLength(120)]
    public string Autor { get; set; } = string.Empty;
    public AreaEmpresa AutorArea { get; set; }

    [Required, StringLength(4000)]
    public string Texto { get; set; } = string.Empty;

    public bool Fixada { get; set; }

    /// <summary>Soft delete (mensagens vinculadas a pagamento nunca são apagadas fisicamente).</summary>
    public bool Excluida { get; set; }
    public bool VinculadaPagamento { get; set; }

    public ICollection<ChatAttachment> Anexos { get; set; } = new List<ChatAttachment>();
    public ICollection<ChatReadReceipt> Leituras { get; set; } = new List<ChatReadReceipt>();
    public ICollection<ChatMention> Mencoes { get; set; } = new List<ChatMention>();
}

public class ChatAttachment : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage? Message { get; set; }

    [Required, StringLength(250)]
    public string NomeArquivo { get; set; } = string.Empty;
    [Required, StringLength(400)]
    public string CaminhoRelativo { get; set; } = string.Empty;
    [StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";
    public long Tamanho { get; set; }
}

public class ChatReadReceipt : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage? Message { get; set; }

    [Required, StringLength(120)]
    public string Usuario { get; set; } = string.Empty;
    public DateTime LidaEm { get; set; } = DateTime.UtcNow;
}

public class ChatMention : BaseEntity
{
    public int MessageId { get; set; }
    public ChatMessage? Message { get; set; }

    [Required, StringLength(120)]
    public string UsuarioMencionado { get; set; } = string.Empty;
}
