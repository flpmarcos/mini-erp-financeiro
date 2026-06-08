using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Contas a Receber: criação, recebimento (total/parcial), inadimplência.
/// Espelho do Contas a Pagar, do lado das entradas.
/// </summary>
public class ContaReceberService : IContaReceberService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public ContaReceberService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<ContaReceber>> ListarAsync(ContaReceberFiltroVM f)
    {
        var query = _db.ContasReceber.AsNoTracking().Include(c => c.Cliente).AsQueryable();
        if (f.Status.HasValue) query = query.Where(c => c.Status == f.Status);
        if (f.ClienteId.HasValue) query = query.Where(c => c.ClienteId == f.ClienteId);
        if (f.VencimentoDe.HasValue) query = query.Where(c => c.DataVencimento >= f.VencimentoDe);
        if (f.VencimentoAte.HasValue) query = query.Where(c => c.DataVencimento <= f.VencimentoAte);
        query = query.OrderBy(c => c.DataVencimento);

        var total = await query.CountAsync();
        var itens = await query.Skip((f.Pagina - 1) * f.TamanhoPagina).Take(f.TamanhoPagina).ToListAsync();
        return new PagedResult<ContaReceber> { Itens = itens, TotalItens = total, Pagina = f.Pagina, TamanhoPagina = f.TamanhoPagina };
    }

    public Task<ContaReceber?> ObterAsync(int id) =>
        _db.ContasReceber.Include(c => c.Cliente).Include(c => c.Categoria)
            .Include(c => c.Recebimentos).ThenInclude(r => r.ContaBancaria)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<OperationResult<ContaReceber>> CriarAsync(ContaReceberFormVM vm, string usuario)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == vm.ClienteId))
            return OperationResult<ContaReceber>.Falha("Cliente invalido.");
        if (vm.Valor <= 0) return OperationResult<ContaReceber>.Falha("Valor deve ser maior que zero.");

        var conta = new ContaReceber
        {
            Descricao = vm.Descricao,
            ClienteId = vm.ClienteId,
            CategoriaId = vm.CategoriaId,
            Valor = vm.Valor,
            DataEmissao = vm.DataEmissao,
            DataVencimento = vm.DataVencimento,
            FormaRecebimento = vm.FormaRecebimento,
            Observacao = vm.Observacao,
            Status = StatusReceber.Aberta
        };
        _db.ContasReceber.Add(conta);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(ContaReceber), conta.Id,
            valorNovo: conta.Valor.ToString("F2"), usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult<ContaReceber>.Ok(conta);
    }

    public async Task<OperationResult> ReceberAsync(RecebimentoVM vm, string usuario)
    {
        var conta = await _db.ContasReceber.FindAsync(vm.ContaReceberId);
        if (conta is null) return OperationResult.Falha("Conta a receber nao encontrada.");
        if (conta.Status == StatusReceber.Cancelada) return OperationResult.Falha("Conta cancelada nao pode receber.");
        if (conta.Status == StatusReceber.Recebida) return OperationResult.Falha("Conta ja foi totalmente recebida.");
        if (vm.ValorRecebido <= 0) return OperationResult.Falha("Valor recebido deve ser maior que zero.");

        _db.Recebimentos.Add(new RecebimentoBaixa
        {
            ContaReceberId = conta.Id,
            DataRecebimento = vm.DataRecebimento,
            ValorRecebido = vm.ValorRecebido,
            ContaBancariaId = vm.ContaBancariaId,
            FormaRecebimento = vm.FormaRecebimento,
            Observacao = vm.Observacao
        });

        conta.ValorRecebido += vm.ValorRecebido;
        conta.DataRecebimento = vm.DataRecebimento;
        conta.Status = conta.ValorRecebido >= conta.Valor
            ? StatusReceber.Recebida
            : StatusReceber.ParcialmenteRecebida;

        await _auditoria.RegistrarAsync(AcaoAuditoria.Pagamento, nameof(ContaReceber), conta.Id,
            "ValorRecebido", null, vm.ValorRecebido.ToString("F2"), usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> CancelarAsync(int id, string usuario)
    {
        var conta = await _db.ContasReceber.FindAsync(id);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");
        if (conta.Status == StatusReceber.Recebida) return OperationResult.Falha("Conta recebida nao pode ser cancelada.");
        conta.Status = StatusReceber.Cancelada;
        await _auditoria.RegistrarAsync(AcaoAuditoria.Cancelamento, nameof(ContaReceber), conta.Id, usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<int> AtualizarVencidasAsync()
    {
        var hoje = DateTime.Today;
        var abertas = await _db.ContasReceber
            .Where(c => c.DataVencimento < hoje
                     && (c.Status == StatusReceber.Aberta || c.Status == StatusReceber.ParcialmenteRecebida))
            .ToListAsync();
        foreach (var c in abertas) c.Status = StatusReceber.Vencida;
        if (abertas.Count > 0) await _db.SaveChangesAsync();
        return abertas.Count;
    }

    public Task<List<ContaReceber>> InadimplentesAsync() =>
        _db.ContasReceber.AsNoTracking().Include(c => c.Cliente)
            .Where(c => c.Status == StatusReceber.Vencida && c.Valor > c.ValorRecebido)
            .OrderBy(c => c.DataVencimento).ToListAsync();
}
