using System.ComponentModel.DataAnnotations;
using FinFlow.Domain.Enums;

namespace FinFlow.Domain.Entities;

/// <summary>Documento anexado a uma conta a pagar (NF, boleto, contrato, comprovante...).</summary>
public class Anexo : BaseEntity
{
    public int ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public TipoAnexo Tipo { get; set; } = TipoAnexo.Outro;

    [Required, StringLength(250)]
    public string NomeArquivo { get; set; } = string.Empty;

    /// <summary>Caminho relativo no storage (disco hoje; S3/MinIO no futuro).</summary>
    [Required, StringLength(400)]
    public string CaminhoRelativo { get; set; } = string.Empty;

    [StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long Tamanho { get; set; }

    [StringLength(120)]
    public string EnviadoPor { get; set; } = "sistema";
}
