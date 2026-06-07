using System.ComponentModel.DataAnnotations;
using ContasAPagar.Web.Domain.Enums;

namespace ContasAPagar.Web.Domain.Entities;

/// <summary>
/// Notificação interna. Destinatario pode ser um usuário (login), um perfil (role)
/// ou "*" (todos). A visibilidade é resolvida pelo NotificacaoService.
/// </summary>
public class Notificacao : BaseEntity
{
    [Required, StringLength(120)]
    public string Destinatario { get; set; } = "*";

    [Required, StringLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Mensagem { get; set; }

    [StringLength(250)]
    public string? Link { get; set; }

    public SeveridadeNotificacao Severidade { get; set; } = SeveridadeNotificacao.Info;

    public bool Lida { get; set; }
    public DateTime? LidaEm { get; set; }
}
