using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;

namespace FinFlow.Services.Interfaces;

public record ArquivoBaixado(byte[] Conteudo, string Nome, string ContentType);

public interface IAnexoService
{
    Task<List<Anexo>> ListarAsync(int contaPagarId);
    Task<OperationResult<Anexo>> UploadAsync(int contaPagarId, string nomeArquivo, string contentType,
        long tamanho, Stream conteudo, TipoAnexo tipo, string usuario);
    Task<ArquivoBaixado?> BaixarAsync(int anexoId);
    Task<OperationResult> RemoverAsync(int anexoId, string usuario);
}
