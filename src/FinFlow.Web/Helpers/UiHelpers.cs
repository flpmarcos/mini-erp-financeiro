using FinFlow.Domain.Enums;

namespace FinFlow.Helpers;

/// <summary>Mapeia status para classes Bootstrap (badges) - usado nas views.</summary>
public static class UiHelpers
{
    public static string BadgeConta(StatusConta status) => status switch
    {
        StatusConta.Paga => "bg-success",
        StatusConta.ParcialmentePaga => "bg-info text-dark",
        StatusConta.Vencida => "bg-danger",
        StatusConta.Pendente => "bg-secondary",
        StatusConta.EmAprovacao => "bg-warning text-dark",
        StatusConta.Aprovada or StatusConta.LiberadaParaPagamento => "bg-primary",
        StatusConta.Reprovada => "bg-danger",
        StatusConta.Cancelada => "bg-dark",
        StatusConta.Estornada => "bg-dark",
        StatusConta.Rascunho => "bg-light text-dark",
        _ => "bg-secondary"
    };

    public static string BadgeReceber(StatusReceber status) => status switch
    {
        StatusReceber.Recebida => "bg-success",
        StatusReceber.ParcialmenteRecebida => "bg-info text-dark",
        StatusReceber.Aberta => "bg-secondary",
        StatusReceber.Vencida => "bg-danger",
        StatusReceber.Cancelada => "bg-dark",
        _ => "bg-secondary"
    };

    public static string BadgeCompra(StatusCompra status) => status switch
    {
        StatusCompra.Recebida => "bg-success",
        StatusCompra.Aprovada => "bg-primary",
        StatusCompra.PedidoEmitido => "bg-info text-dark",
        StatusCompra.Solicitada => "bg-secondary",
        StatusCompra.Reprovada => "bg-danger",
        StatusCompra.Cancelada => "bg-dark",
        _ => "bg-secondary"
    };

    public static string BadgeConciliacao(StatusConciliacao status) => status switch
    {
        StatusConciliacao.Conciliado => "bg-success",
        StatusConciliacao.Divergente => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    public static string BadgeTransacao(StatusTransacaoBancaria status) => status switch
    {
        StatusTransacaoBancaria.Sucesso => "bg-success",
        StatusTransacaoBancaria.Pendente => "bg-warning text-dark",
        StatusTransacaoBancaria.Erro => "bg-danger",
        _ => "bg-secondary"
    };
}
