using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Nucleo do dominio: criacao, edicao, cancelamento, parcelamento e retencao.
/// Mantem a Controller fina - toda regra de negocio fica aqui.
/// </summary>
public class ContaPagarService : IContaPagarService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public ContaPagarService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<ContaPagar>> ListarAsync(ContaPagarFiltroVM f)
    {
        var query = _db.ContasPagar.AsNoTracking()
            .Include(c => c.Fornecedor)
            .Include(c => c.Categoria)
            .Include(c => c.CentroCusto)
            .Where(c => c.NumeroParcela != 0) // esconde a conta-mae de parcelamento
            .AsQueryable();

        if (f.Status.HasValue) query = query.Where(c => c.Status == f.Status);
        if (f.FornecedorId.HasValue) query = query.Where(c => c.FornecedorId == f.FornecedorId);
        if (f.CentroCustoId.HasValue) query = query.Where(c => c.CentroCustoId == f.CentroCustoId);
        if (f.VencimentoDe.HasValue) query = query.Where(c => c.DataVencimento >= f.VencimentoDe);
        if (f.VencimentoAte.HasValue) query = query.Where(c => c.DataVencimento <= f.VencimentoAte);

        query = query.OrderBy(c => c.DataVencimento);

        var total = await query.CountAsync();
        var itens = await query.Skip((f.Pagina - 1) * f.TamanhoPagina).Take(f.TamanhoPagina).ToListAsync();
        return new PagedResult<ContaPagar> { Itens = itens, TotalItens = total, Pagina = f.Pagina, TamanhoPagina = f.TamanhoPagina };
    }

    public Task<ContaPagar?> ObterAsync(int id) =>
        _db.ContasPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.Categoria)
            .Include(c => c.CentroCusto)
            .Include(c => c.Retencoes)
            .Include(c => c.Aprovacoes)
            .Include(c => c.Baixas).ThenInclude(b => b.ContaBancaria)
            .Include(c => c.Transacoes)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<ContaPagar>> ListarParcelasAsync(int contaOrigemId) =>
        await _db.ContasPagar.AsNoTracking()
            .Where(c => c.ContaOrigemId == contaOrigemId)
            .OrderBy(c => c.NumeroParcela)
            .ToListAsync();

    public async Task<OperationResult<ContaPagar>> CriarAsync(ContaPagarFormVM vm, string usuario)
    {
        var erro = await ValidarCadastrosAsync(vm.FornecedorId, vm.CategoriaId, vm.CentroCustoId);
        if (erro is not null) return OperationResult<ContaPagar>.Falha(erro);
        if (vm.ValorOriginal <= 0) return OperationResult<ContaPagar>.Falha("Valor deve ser maior que zero.");

        var conta = new ContaPagar
        {
            Descricao = vm.Descricao,
            FornecedorId = vm.FornecedorId,
            CategoriaId = vm.CategoriaId,
            CentroCustoId = vm.CentroCustoId,
            ValorOriginal = vm.ValorOriginal,
            DataEmissao = vm.DataEmissao,
            DataCompetencia = vm.DataCompetencia,
            DataVencimento = vm.DataVencimento,
            FormaPagamento = vm.FormaPagamento,
            CodigoBarras = vm.CodigoBarras,
            ChavePix = vm.ChavePix,
            Observacao = vm.Observacao,
            Status = StatusConta.Pendente
        };

        AplicarRetencoes(conta, vm.Retencoes);

        _db.ContasPagar.Add(conta);
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(ContaPagar), conta.Id,
            valorNovo: conta.ValorOriginal.ToString("F2"), usuario: usuario);
        await _db.SaveChangesAsync();

        return OperationResult<ContaPagar>.Ok(conta);
    }

    public async Task<OperationResult> AtualizarAsync(ContaPagarFormVM vm, string usuario)
    {
        var conta = await _db.ContasPagar.Include(c => c.Retencoes).FirstOrDefaultAsync(c => c.Id == vm.Id);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");

        // Regra: conta paga/cancelada/estornada nao pode ser editada livremente.
        if (conta.Status is StatusConta.Paga or StatusConta.Cancelada or StatusConta.Estornada or StatusConta.ParcialmentePaga)
            return OperationResult.Falha($"Conta com status '{conta.Status}' nao pode ser editada.");

        var erro = await ValidarCadastrosAsync(vm.FornecedorId, vm.CategoriaId, vm.CentroCustoId);
        if (erro is not null) return OperationResult.Falha(erro);

        // Auditoria de campos sensiveis
        if (conta.ValorOriginal != vm.ValorOriginal)
            await _auditoria.RegistrarAsync(AcaoAuditoria.EdicaoValor, nameof(ContaPagar), conta.Id,
                "ValorOriginal", conta.ValorOriginal.ToString("F2"), vm.ValorOriginal.ToString("F2"), usuario);
        if (conta.DataVencimento != vm.DataVencimento)
            await _auditoria.RegistrarAsync(AcaoAuditoria.AlteracaoVencimento, nameof(ContaPagar), conta.Id,
                "DataVencimento", conta.DataVencimento.ToString("yyyy-MM-dd"), vm.DataVencimento.ToString("yyyy-MM-dd"), usuario);

        conta.Descricao = vm.Descricao;
        conta.FornecedorId = vm.FornecedorId;
        conta.CategoriaId = vm.CategoriaId;
        conta.CentroCustoId = vm.CentroCustoId;
        conta.ValorOriginal = vm.ValorOriginal;
        conta.DataEmissao = vm.DataEmissao;
        conta.DataCompetencia = vm.DataCompetencia;
        conta.DataVencimento = vm.DataVencimento;
        conta.FormaPagamento = vm.FormaPagamento;
        conta.CodigoBarras = vm.CodigoBarras;
        conta.ChavePix = vm.ChavePix;
        conta.Observacao = vm.Observacao;

        // Recalcula retencoes
        _db.Retencoes.RemoveRange(conta.Retencoes);
        conta.Retencoes.Clear();
        AplicarRetencoes(conta, vm.Retencoes);

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> CancelarAsync(int id, string usuario)
    {
        var conta = await _db.ContasPagar.FindAsync(id);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");
        if (conta.Status == StatusConta.Paga) return OperationResult.Falha("Conta paga nao pode ser cancelada.");
        if (conta.Status == StatusConta.Cancelada) return OperationResult.Falha("Conta ja esta cancelada.");

        conta.Status = StatusConta.Cancelada;
        await _auditoria.RegistrarAsync(AcaoAuditoria.Cancelamento, nameof(ContaPagar), conta.Id, usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult<ContaPagar>> GerarParcelamentoAsync(ParcelamentoVM vm, string usuario)
    {
        var erro = await ValidarCadastrosAsync(vm.FornecedorId, vm.CategoriaId, vm.CentroCustoId);
        if (erro is not null) return OperationResult<ContaPagar>.Falha(erro);
        if (vm.ValorTotal <= 0) return OperationResult<ContaPagar>.Falha("Valor total deve ser maior que zero.");
        if (vm.Parcelas < 2) return OperationResult<ContaPagar>.Falha("Parcelamento exige ao menos 2 parcelas.");

        // Conta-mae (controle): NumeroParcela = 0, nao aparece nas listagens.
        var origem = new ContaPagar
        {
            Descricao = $"{vm.Descricao} (compra parcelada {vm.Parcelas}x)",
            FornecedorId = vm.FornecedorId,
            CategoriaId = vm.CategoriaId,
            CentroCustoId = vm.CentroCustoId,
            ValorOriginal = vm.ValorTotal,
            ValorLiquido = vm.ValorTotal,
            DataEmissao = DateTime.Today,
            DataCompetencia = DateTime.Today,
            DataVencimento = vm.PrimeiroVencimento,
            FormaPagamento = vm.FormaPagamento,
            Status = StatusConta.Pendente,
            NumeroParcela = 0,
            TotalParcelas = vm.Parcelas
        };
        _db.ContasPagar.Add(origem);
        await _db.SaveChangesAsync();

        // Divide o valor; ultima parcela absorve a diferenca de centavos.
        var valorParcela = Math.Round(vm.ValorTotal / vm.Parcelas, 2);
        var acumulado = 0m;

        for (int p = 1; p <= vm.Parcelas; p++)
        {
            var valor = p == vm.Parcelas ? vm.ValorTotal - acumulado : valorParcela;
            acumulado += valor;

            _db.ContasPagar.Add(new ContaPagar
            {
                Descricao = $"{vm.Descricao} - parcela {p}/{vm.Parcelas}",
                FornecedorId = vm.FornecedorId,
                CategoriaId = vm.CategoriaId,
                CentroCustoId = vm.CentroCustoId,
                ValorOriginal = valor,
                ValorLiquido = valor,
                DataEmissao = DateTime.Today,
                DataCompetencia = DateTime.Today,
                DataVencimento = vm.PrimeiroVencimento.AddMonths(p - 1),
                FormaPagamento = vm.FormaPagamento,
                Status = StatusConta.Pendente,
                ContaOrigemId = origem.Id,
                NumeroParcela = p,
                TotalParcelas = vm.Parcelas
            });
        }

        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(ContaPagar), origem.Id,
            valorNovo: $"Parcelamento {vm.Parcelas}x de {vm.ValorTotal:F2}", usuario: usuario);
        await _db.SaveChangesAsync();

        return OperationResult<ContaPagar>.Ok(origem);
    }

    public async Task<int> AtualizarVencidasAsync()
    {
        var hoje = DateTime.Today;
        var pendentes = await _db.ContasPagar
            .Where(c => c.DataVencimento < hoje
                     && (c.Status == StatusConta.Pendente
                      || c.Status == StatusConta.Aprovada
                      || c.Status == StatusConta.LiberadaParaPagamento
                      || c.Status == StatusConta.EmAprovacao))
            .ToListAsync();

        foreach (var c in pendentes)
            c.Status = StatusConta.Vencida;

        if (pendentes.Count > 0) await _db.SaveChangesAsync();
        return pendentes.Count;
    }

    // ---- helpers privados ----

    /// <summary>Calcula valor de cada retencao e o valor liquido da conta.</summary>
    private static void AplicarRetencoes(ContaPagar conta, IEnumerable<RetencaoInputVM> retencoes)
    {
        decimal totalImpostos = 0m;
        foreach (var r in retencoes.Where(r => r.Aliquota > 0))
        {
            var valor = Math.Round(conta.ValorOriginal * (r.Aliquota / 100m), 2);
            totalImpostos += valor;
            conta.Retencoes.Add(new RetencaoImposto { Tipo = r.Tipo, Aliquota = r.Aliquota, Valor = valor });
        }
        conta.ValorLiquido = conta.ValorOriginal - totalImpostos;
    }

    private async Task<string?> ValidarCadastrosAsync(int fornecedorId, int categoriaId, int centroCustoId)
    {
        if (!await _db.Fornecedores.AnyAsync(f => f.Id == fornecedorId))
            return "Fornecedor invalido.";
        if (!await _db.Categorias.AnyAsync(c => c.Id == categoriaId))
            return "Categoria invalida.";
        if (!await _db.CentrosCusto.AnyAsync(c => c.Id == centroCustoId))
            return "Centro de custo invalido.";
        return null;
    }
}
