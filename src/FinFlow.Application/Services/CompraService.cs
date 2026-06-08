using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Módulo de Compras (Fase 8): solicitação → aprovação → pedido → recebimento.
/// A Conta a Pagar só é gerada no recebimento (regra: sem recebimento, sem AP).
/// </summary>
public class CompraService : ICompraService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public CompraService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public Task<List<SolicitacaoCompra>> ListarAsync() =>
        _db.SolicitacoesCompra.AsNoTracking()
            .Include(s => s.Fornecedor).Include(s => s.Categoria).Include(s => s.CentroCusto)
            .OrderByDescending(s => s.CriadoEm).ToListAsync();

    public Task<SolicitacaoCompra?> ObterAsync(int id) =>
        _db.SolicitacoesCompra
            .Include(s => s.Fornecedor).Include(s => s.Categoria).Include(s => s.CentroCusto)
            .Include(s => s.ContaPagarGerada)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<OperationResult<SolicitacaoCompra>> CriarAsync(CompraFormVM vm, string usuario)
    {
        if (!await _db.Fornecedores.AnyAsync(f => f.Id == vm.FornecedorId)) return OperationResult<SolicitacaoCompra>.Falha("Fornecedor invalido.");
        if (!await _db.Categorias.AnyAsync(c => c.Id == vm.CategoriaId)) return OperationResult<SolicitacaoCompra>.Falha("Categoria invalida.");
        if (!await _db.CentrosCusto.AnyAsync(c => c.Id == vm.CentroCustoId)) return OperationResult<SolicitacaoCompra>.Falha("Centro de custo invalido.");
        if (vm.ValorEstimado <= 0) return OperationResult<SolicitacaoCompra>.Falha("Valor estimado deve ser maior que zero.");

        var s = new SolicitacaoCompra
        {
            Descricao = vm.Descricao,
            FornecedorId = vm.FornecedorId,
            CategoriaId = vm.CategoriaId,
            CentroCustoId = vm.CentroCustoId,
            ValorEstimado = vm.ValorEstimado,
            Justificativa = vm.Justificativa,
            SolicitadoPor = usuario,
            Status = StatusCompra.Solicitada
        };
        _db.SolicitacoesCompra.Add(s);
        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(SolicitacaoCompra), 0,
            valorNovo: vm.Descricao, usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult<SolicitacaoCompra>.Ok(s);
    }

    public Task<OperationResult> AprovarAsync(int id, string usuario) =>
        TransicionarAsync(id, StatusCompra.Solicitada, StatusCompra.Aprovada, AcaoAuditoria.Aprovacao, usuario);

    public Task<OperationResult> ReprovarAsync(int id, string usuario) =>
        TransicionarAsync(id, StatusCompra.Solicitada, StatusCompra.Reprovada, AcaoAuditoria.Reprovacao, usuario);

    public Task<OperationResult> EmitirPedidoAsync(int id, string usuario) =>
        TransicionarAsync(id, StatusCompra.Aprovada, StatusCompra.PedidoEmitido, AcaoAuditoria.EdicaoValor, usuario);

    public async Task<OperationResult<ContaPagar>> ReceberAsync(int id, string usuario)
    {
        var s = await _db.SolicitacoesCompra.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return OperationResult<ContaPagar>.Falha("Solicitacao nao encontrada.");
        if (s.Status != StatusCompra.PedidoEmitido)
            return OperationResult<ContaPagar>.Falha("Só é possível receber após o pedido ter sido emitido.");

        // Gera a Conta a Pagar a partir da compra recebida.
        var conta = new ContaPagar
        {
            Descricao = $"Compra: {s.Descricao}",
            FornecedorId = s.FornecedorId,
            CategoriaId = s.CategoriaId,
            CentroCustoId = s.CentroCustoId,
            ValorOriginal = s.ValorEstimado,
            ValorLiquido = s.ValorEstimado,
            DataEmissao = DateTime.Today,
            DataCompetencia = DateTime.Today,
            DataVencimento = DateTime.Today.AddDays(30),
            FormaPagamento = FormaPagamento.Boleto,
            Status = StatusConta.Pendente
        };
        _db.ContasPagar.Add(conta);
        await _db.SaveChangesAsync();

        s.Status = StatusCompra.Recebida;
        s.DataRecebimento = DateTime.Today;
        s.ContaPagarGeradaId = conta.Id;
        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(ContaPagar), conta.Id,
            valorNovo: $"Gerada da compra {s.Id}", usuario: usuario, motivo: "Recebimento de compra");
        await _db.SaveChangesAsync();
        return OperationResult<ContaPagar>.Ok(conta);
    }

    public async Task<OperationResult> CancelarAsync(int id, string usuario)
    {
        var s = await _db.SolicitacoesCompra.FindAsync(id);
        if (s is null) return OperationResult.Falha("Solicitacao nao encontrada.");
        if (s.Status == StatusCompra.Recebida) return OperationResult.Falha("Compra recebida nao pode ser cancelada.");
        s.Status = StatusCompra.Cancelada;
        await _auditoria.RegistrarAsync(AcaoAuditoria.Cancelamento, nameof(SolicitacaoCompra), s.Id, usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    private async Task<OperationResult> TransicionarAsync(int id, StatusCompra de, StatusCompra para, AcaoAuditoria acao, string usuario)
    {
        var s = await _db.SolicitacoesCompra.FindAsync(id);
        if (s is null) return OperationResult.Falha("Solicitacao nao encontrada.");
        if (s.Status != de) return OperationResult.Falha($"Transicao invalida: status atual e '{s.Status}', esperado '{de}'.");
        s.Status = para;
        await _auditoria.RegistrarAsync(acao, nameof(SolicitacaoCompra), s.Id, "Status", de.ToString(), para.ToString(), usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }
}
