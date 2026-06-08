using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Contabilidade: plano de contas + lançamentos por partida dobrada + balancete,
/// razão e DRE. Também faz o lançamento AUTOMÁTICO a partir de pagamentos/recebimentos.
/// </summary>
public class ContabilidadeService : IContabilidadeService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public ContabilidadeService(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public Task<List<ContaContabil>> ListarPlanoAsync() =>
        _db.ContasContabeis.AsNoTracking().OrderBy(c => c.Codigo).ToListAsync();

    public Task<ContaContabil?> ObterContaAsync(int id) => _db.ContasContabeis.FindAsync(id).AsTask();

    public async Task<OperationResult<ContaContabil>> CriarContaAsync(ContaContabil conta)
    {
        if (string.IsNullOrWhiteSpace(conta.Codigo) || string.IsNullOrWhiteSpace(conta.Nome))
            return OperationResult<ContaContabil>.Falha("Código e nome são obrigatórios.");
        if (await _db.ContasContabeis.AnyAsync(c => c.Codigo == conta.Codigo))
            return OperationResult<ContaContabil>.Falha("Já existe conta com este código.");
        _db.ContasContabeis.Add(conta);
        await _db.SaveChangesAsync();
        return OperationResult<ContaContabil>.Ok(conta);
    }

    public Task<List<LancamentoContabil>> ListarLancamentosAsync(int take = 100) =>
        _db.Lancamentos.AsNoTracking().Include(l => l.Partidas).ThenInclude(p => p.Conta)
            .OrderByDescending(l => l.Data).ThenByDescending(l => l.Id).Take(take).ToListAsync();

    public Task<LancamentoContabil?> ObterLancamentoAsync(int id) =>
        _db.Lancamentos.Include(l => l.Partidas).ThenInclude(p => p.Conta)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<OperationResult<LancamentoContabil>> CriarLancamentoAsync(LancamentoFormVM vm, string usuario, string origem = "Manual")
    {
        var partidas = vm.Partidas.Where(p => p.Valor > 0 && p.ContaContabilId > 0).ToList();
        if (partidas.Count < 2)
            return OperationResult<LancamentoContabil>.Falha("Um lançamento exige ao menos 2 partidas (débito e crédito).");

        var debitos = partidas.Where(p => p.Tipo == TipoPartida.Debito).Sum(p => p.Valor);
        var creditos = partidas.Where(p => p.Tipo == TipoPartida.Credito).Sum(p => p.Valor);
        if (debitos != creditos)
            return OperationResult<LancamentoContabil>.Falha($"Lançamento não balanceado: débitos {debitos:C} ≠ créditos {creditos:C}.");
        if (debitos == 0m)
            return OperationResult<LancamentoContabil>.Falha("Valor do lançamento deve ser maior que zero.");

        // Só permite lançar em contas analíticas existentes.
        var ids = partidas.Select(p => p.ContaContabilId).Distinct().ToList();
        var contas = await _db.ContasContabeis.Where(c => ids.Contains(c.Id)).ToListAsync();
        if (contas.Count != ids.Count || contas.Any(c => !c.Analitica))
            return OperationResult<LancamentoContabil>.Falha("Há conta inexistente ou sintética (não aceita lançamento).");

        var lanc = new LancamentoContabil { Data = vm.Data, Historico = vm.Historico, Origem = origem };
        foreach (var p in partidas)
            lanc.Partidas.Add(new PartidaContabil { ContaContabilId = p.ContaContabilId, Tipo = p.Tipo, Valor = p.Valor });

        _db.Lancamentos.Add(lanc);
        await _db.SaveChangesAsync();
        // Audit após o save: o Id já foi gerado.
        await _auditoria.RegistrarAsync(AcaoAuditoria.Criacao, nameof(LancamentoContabil), lanc.Id,
            valorNovo: $"{vm.Historico} ({debitos:C})", usuario: usuario);
        await _db.SaveChangesAsync();
        return OperationResult<LancamentoContabil>.Ok(lanc);
    }

    public async Task<List<BalanceteLinha>> BalanceteAsync()
    {
        var contas = await _db.ContasContabeis.AsNoTracking().Where(c => c.Analitica).OrderBy(c => c.Codigo).ToListAsync();
        var partidas = await _db.Partidas.AsNoTracking().Include(p => p.Lancamento).ToListAsync();

        var linhas = new List<BalanceteLinha>();
        foreach (var c in contas)
        {
            var doConta = partidas.Where(p => p.ContaContabilId == c.Id).ToList();
            var deb = doConta.Where(p => p.Tipo == TipoPartida.Debito).Sum(p => p.Valor);
            var cred = doConta.Where(p => p.Tipo == TipoPartida.Credito).Sum(p => p.Valor);
            // Saldo conforme natureza: devedora = D − C; credora = C − D.
            var saldo = c.Natureza == NaturezaConta.Devedora ? deb - cred : cred - deb;
            if (deb != 0 || cred != 0)
                linhas.Add(new BalanceteLinha(c.Codigo, c.Nome, c.Tipo.ToString(), deb, cred, saldo));
        }
        return linhas;
    }

    public async Task<List<RazaoMovimento>> RazaoAsync(int contaContabilId)
    {
        var conta = await _db.ContasContabeis.FindAsync(contaContabilId);
        if (conta is null) return new();

        var movimentos = await _db.Partidas.AsNoTracking().Include(p => p.Lancamento)
            .Where(p => p.ContaContabilId == contaContabilId)
            .OrderBy(p => p.Lancamento!.Data).ThenBy(p => p.Id)
            .ToListAsync();

        var razao = new List<RazaoMovimento>();
        decimal saldo = 0m;
        foreach (var p in movimentos)
        {
            var deb = p.Tipo == TipoPartida.Debito ? p.Valor : 0m;
            var cred = p.Tipo == TipoPartida.Credito ? p.Valor : 0m;
            saldo += conta.Natureza == NaturezaConta.Devedora ? deb - cred : cred - deb;
            razao.Add(new RazaoMovimento(p.Lancamento!.Data, p.Lancamento.Historico, deb, cred, saldo));
        }
        return razao;
    }

    public async Task<DreResultado> DreAsync()
    {
        var balancete = await BalanceteAsync();
        var receitas = balancete.Where(l => l.Tipo == TipoContaContabil.Receita.ToString()).Sum(l => l.Saldo);
        var despesas = balancete.Where(l => l.Tipo == TipoContaContabil.Despesa.ToString()).Sum(l => l.Saldo);
        return new DreResultado(receitas, despesas);
    }

    public async Task LancarPagamentoAsync(int contaPagarId, decimal valor, string usuario)
    {
        // Pagamento: D Despesas, C Bancos.
        await LancarAutomaticoAsync(PlanoContasPadrao.Despesas, PlanoContasPadrao.Bancos, valor,
            $"Pagamento da conta a pagar #{contaPagarId}", $"Pagamento:{contaPagarId}", usuario);
    }

    public async Task LancarRecebimentoAsync(int contaReceberId, decimal valor, string usuario)
    {
        // Recebimento: D Bancos, C Receitas.
        await LancarAutomaticoAsync(PlanoContasPadrao.Bancos, PlanoContasPadrao.Receitas, valor,
            $"Recebimento da conta a receber #{contaReceberId}", $"Recebimento:{contaReceberId}", usuario);
    }

    public Task EstornarPagamentoAsync(int contaPagarId, string usuario) =>
        EstornarAutomaticoAsync($"Pagamento:{contaPagarId}", usuario);

    public Task EstornarRecebimentoAsync(int contaReceberId, string usuario) =>
        EstornarAutomaticoAsync($"Recebimento:{contaReceberId}", usuario);

    /// <summary>
    /// Reverte os lançamentos automáticos de uma origem (ex.: "Pagamento:42") criando
    /// lançamentos com as partidas invertidas. Idempotente: não estorna duas vezes.
    /// </summary>
    private async Task EstornarAutomaticoAsync(string origemOriginal, string usuario)
    {
        var origemEstorno = $"Estorno:{origemOriginal}";
        if (await _db.Lancamentos.AnyAsync(l => l.Origem == origemEstorno))
            return; // já estornado

        var originais = await _db.Lancamentos.Include(l => l.Partidas)
            .Where(l => l.Origem == origemOriginal).ToListAsync();
        if (originais.Count == 0) return; // nada lançado (plano não seedado) → nada a reverter

        foreach (var orig in originais)
        {
            var estorno = new LancamentoContabil
            {
                Data = DateTime.UtcNow.Date,
                Historico = $"Estorno: {orig.Historico}",
                Origem = origemEstorno,
            };
            foreach (var p in orig.Partidas)
                estorno.Partidas.Add(new PartidaContabil
                {
                    ContaContabilId = p.ContaContabilId,
                    Tipo = p.Tipo == TipoPartida.Debito ? TipoPartida.Credito : TipoPartida.Debito,
                    Valor = p.Valor,
                });
            _db.Lancamentos.Add(estorno);
        }

        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync(AcaoAuditoria.Estorno, nameof(LancamentoContabil), 0,
            valorNovo: $"Estorno contábil de {origemOriginal}", usuario: usuario);
        await _db.SaveChangesAsync();
    }

    private async Task LancarAutomaticoAsync(string codDebito, string codCredito, decimal valor,
        string historico, string origem, string usuario)
    {
        if (valor <= 0) return;
        var contas = await _db.ContasContabeis
            .Where(c => c.Codigo == codDebito || c.Codigo == codCredito).ToListAsync();
        var debito = contas.FirstOrDefault(c => c.Codigo == codDebito);
        var credito = contas.FirstOrDefault(c => c.Codigo == codCredito);
        if (debito is null || credito is null) return; // plano não seedado → não bloqueia o financeiro

        _db.Lancamentos.Add(new LancamentoContabil
        {
            Data = DateTime.Today,
            Historico = historico,
            Origem = origem,
            Partidas =
            {
                new PartidaContabil { ContaContabilId = debito.Id, Tipo = TipoPartida.Debito, Valor = valor },
                new PartidaContabil { ContaContabilId = credito.Id, Tipo = TipoPartida.Credito, Valor = valor },
            }
        });
        await _db.SaveChangesAsync();
    }
}
