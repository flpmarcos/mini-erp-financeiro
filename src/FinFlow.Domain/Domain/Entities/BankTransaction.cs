using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>
/// Transacao bancaria simulada. Registra envio/resposta da integracao fake.
/// Estruturada para no futuro trocar o servico fake por um adapter de banco real.
/// </summary>
public class BankTransaction : BaseEntity
{
    public int ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public BancoIntegracao Banco { get; set; }
    public FormaPagamento TipoPagamento { get; set; }
    public StatusTransacaoBancaria Status { get; set; } = StatusTransacaoBancaria.Pendente;

    /// <summary>Codigo/identificador retornado pelo banco (E2E id, NSU...).</summary>
    [StringLength(80)]
    public string? CodigoTransacao { get; set; }

    public decimal Valor { get; set; }

    /// <summary>Corpo da requisicao enviada (log, normalmente JSON).</summary>
    public string? PayloadEnvio { get; set; }
    /// <summary>Corpo da resposta recebida (log, normalmente JSON).</summary>
    public string? PayloadResposta { get; set; }

    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    public DateTime? DataRetorno { get; set; }

    [StringLength(500)]
    public string? MensagemErro { get; set; }

    public StatusConciliacao StatusConciliacao { get; set; } = StatusConciliacao.NaoConciliado;
    public DateTime? DataConciliacao { get; set; }
}
