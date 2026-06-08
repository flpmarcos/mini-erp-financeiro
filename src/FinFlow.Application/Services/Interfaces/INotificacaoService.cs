using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

public interface INotificacaoService
{
    /// <summary>Cria notificação interna e dispara os canais fake (e-mail/WhatsApp).</summary>
    Task NotificarAsync(string destinatario, string titulo, string? mensagem = null,
        SeveridadeNotificacao severidade = SeveridadeNotificacao.Info, string? link = null);

    Task<List<Notificacao>> ListarAsync(string usuario, IEnumerable<string> roles, bool somenteNaoLidas, int take = 50);
    Task<int> ContarNaoLidasAsync(string usuario, IEnumerable<string> roles);
    Task<OperationResult> MarcarLidaAsync(int id, string usuario, IEnumerable<string> roles);
    Task MarcarTodasLidasAsync(string usuario, IEnumerable<string> roles);
}
