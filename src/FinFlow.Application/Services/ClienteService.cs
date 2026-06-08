using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>Regras de cliente (espelho leve do fornecedor) para Contas a Receber.</summary>
public class ClienteService : IClienteService
{
    private readonly AppDbContext _db;
    public ClienteService(AppDbContext db) => _db = db;

    public async Task<PagedResult<Cliente>> ListarAsync(string? busca, int pagina, int tamanho)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            query = query.Where(c => c.RazaoSocial.Contains(b) || c.Documento.Contains(b)
                                  || (c.NomeFantasia != null && c.NomeFantasia.Contains(b)));
        }
        query = query.OrderBy(c => c.RazaoSocial);
        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PagedResult<Cliente> { Itens = itens, TotalItens = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    public Task<List<Cliente>> ListarAtivosAsync() =>
        _db.Clientes.AsNoTracking().Where(c => c.Status == StatusFornecedor.Ativo)
            .OrderBy(c => c.RazaoSocial).ToListAsync();

    public Task<Cliente?> ObterAsync(int id) => _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<OperationResult<Cliente>> CriarAsync(Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.RazaoSocial))
            return OperationResult<Cliente>.Falha("Razao social e obrigatoria.");

        c.Documento = DocumentoValidator.SomenteDigitos(c.Documento);
        var docOk = c.TipoDocumento == TipoDocumento.Cpf
            ? DocumentoValidator.ValidarCpf(c.Documento)
            : DocumentoValidator.ValidarCnpj(c.Documento);
        if (!docOk) return OperationResult<Cliente>.Falha("Documento (CPF/CNPJ) invalido.");

        if (await _db.Clientes.AnyAsync(x => x.Documento == c.Documento))
            return OperationResult<Cliente>.Falha("Ja existe cliente com este documento.");

        _db.Clientes.Add(c);
        await _db.SaveChangesAsync();
        return OperationResult<Cliente>.Ok(c);
    }

    public async Task<OperationResult> AtualizarAsync(Cliente c)
    {
        var atual = await _db.Clientes.FindAsync(c.Id);
        if (atual is null) return OperationResult.Falha("Cliente nao encontrado.");
        atual.RazaoSocial = c.RazaoSocial;
        atual.NomeFantasia = c.NomeFantasia;
        atual.TipoDocumento = c.TipoDocumento;
        atual.Documento = DocumentoValidator.SomenteDigitos(c.Documento);
        atual.Email = c.Email;
        atual.Telefone = c.Telefone;
        atual.Endereco = c.Endereco;
        atual.Status = c.Status;
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
