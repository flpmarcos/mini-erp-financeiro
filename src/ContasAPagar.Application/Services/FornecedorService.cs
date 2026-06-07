using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Repositories;
using ContasAPagar.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Regras de fornecedor. Demonstra o uso do Repository generico.
/// Valida nome obrigatorio e documento (CPF/CNPJ) de forma simples.
/// </summary>
public class FornecedorService : IFornecedorService
{
    private readonly IRepository<Fornecedor> _repo;

    public FornecedorService(IRepository<Fornecedor> repo) => _repo = repo;

    public async Task<PagedResult<Fornecedor>> ListarAsync(string? busca, int pagina, int tamanho)
    {
        var query = _repo.Query(tracking: false);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim();
            query = query.Where(f => f.RazaoSocial.Contains(b)
                                  || (f.NomeFantasia != null && f.NomeFantasia.Contains(b))
                                  || f.Documento.Contains(b));
        }

        query = query.OrderBy(f => f.RazaoSocial);
        var total = await query.CountAsync();
        var itens = await query.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync();
        return new PagedResult<Fornecedor> { Itens = itens, TotalItens = total, Pagina = pagina, TamanhoPagina = tamanho };
    }

    public Task<Fornecedor?> ObterAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<List<Fornecedor>> ListarAtivosAsync() =>
        await _repo.Query(tracking: false)
                   .Where(f => f.Status == StatusFornecedor.Ativo)
                   .OrderBy(f => f.RazaoSocial)
                   .ToListAsync();

    public async Task<OperationResult<Fornecedor>> CriarAsync(Fornecedor f)
    {
        var validacao = Validar(f);
        if (validacao is not null) return OperationResult<Fornecedor>.Falha(validacao);

        f.Documento = DocumentoValidator.SomenteDigitos(f.Documento);
        if (await _repo.Query(false).AnyAsync(x => x.Documento == f.Documento))
            return OperationResult<Fornecedor>.Falha("Ja existe fornecedor com este documento.");

        await _repo.AddAsync(f);
        await _repo.SaveChangesAsync();
        return OperationResult<Fornecedor>.Ok(f);
    }

    public async Task<OperationResult> AtualizarAsync(Fornecedor f)
    {
        var validacao = Validar(f);
        if (validacao is not null) return OperationResult.Falha(validacao);

        var atual = await _repo.GetByIdAsync(f.Id);
        if (atual is null) return OperationResult.Falha("Fornecedor nao encontrado.");

        atual.RazaoSocial = f.RazaoSocial;
        atual.NomeFantasia = f.NomeFantasia;
        atual.TipoDocumento = f.TipoDocumento;
        atual.Documento = DocumentoValidator.SomenteDigitos(f.Documento);
        atual.Email = f.Email;
        atual.Telefone = f.Telefone;
        atual.Endereco = f.Endereco;
        atual.Banco = f.Banco;
        atual.Agencia = f.Agencia;
        atual.Conta = f.Conta;
        atual.TipoConta = f.TipoConta;
        atual.ChavePix = f.ChavePix;
        atual.Status = f.Status;

        _repo.Update(atual);
        await _repo.SaveChangesAsync();
        return OperationResult.Ok();
    }

    /// <summary>Retorna mensagem de erro ou null se valido.</summary>
    private static string? Validar(Fornecedor f)
    {
        if (string.IsNullOrWhiteSpace(f.RazaoSocial))
            return "Razao social e obrigatoria.";

        var doc = DocumentoValidator.SomenteDigitos(f.Documento);
        var ok = f.TipoDocumento == TipoDocumento.Cpf
            ? DocumentoValidator.ValidarCpf(doc)
            : DocumentoValidator.ValidarCnpj(doc);

        return ok ? null : $"{f.TipoDocumento.ToString().ToUpper()} invalido.";
    }
}
