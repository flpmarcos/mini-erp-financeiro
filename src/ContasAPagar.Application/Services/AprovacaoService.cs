using ContasAPagar.Web.Configurations;
using ContasAPagar.Web.Data;
using ContasAPagar.Web.Domain.Entities;
using ContasAPagar.Web.Domain.Enums;
using ContasAPagar.Web.Helpers;
using ContasAPagar.Web.Services.Interfaces;
using ContasAPagar.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContasAPagar.Web.Services;

/// <summary>
/// Fluxo de aprovacao por alcada:
/// &lt; R$ 1.000 -> aprovacao automatica;
/// R$ 1.000 a R$ 10.000 -> gerente;
/// &gt; R$ 10.000 -> diretor.
/// </summary>
public class AprovacaoService : IAprovacaoService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;
    private readonly IRegraAprovacaoService _regras;
    private readonly INotificacaoService _notificacoes;
    private readonly FinanceiroOptions _opt;

    public AprovacaoService(AppDbContext db, IAuditoriaService auditoria,
        IRegraAprovacaoService regras, INotificacaoService notificacoes, IOptions<FinanceiroOptions> opt)
    {
        _db = db;
        _auditoria = auditoria;
        _regras = regras;
        _notificacoes = notificacoes;
        _opt = opt.Value;
    }

    public NivelAprovacao DeterminarNivel(decimal valor)
    {
        if (valor < _opt.LimiteAprovacaoAutomatica) return NivelAprovacao.Automatica;
        if (valor <= _opt.LimiteAprovacaoGerente) return NivelAprovacao.Gerente;
        return NivelAprovacao.Diretor;
    }

    public async Task<OperationResult> EnviarParaAprovacaoAsync(int contaId, string usuario)
    {
        var conta = await _db.ContasPagar.FindAsync(contaId);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");
        if (conta.Status is not (StatusConta.Pendente or StatusConta.Rascunho or StatusConta.Reprovada or StatusConta.Vencida))
            return OperationResult.Falha($"Conta com status '{conta.Status}' nao pode entrar em aprovacao.");

        // Regra configurável tem prioridade; sem regra que case, usa a alçada por valor.
        var nivel = await _regras.ResolverNivelAsync(conta.ValorLiquido, conta.CategoriaId, conta.CentroCustoId, conta.FornecedorId)
                    ?? DeterminarNivel(conta.ValorLiquido);

        if (nivel == NivelAprovacao.Automatica)
        {
            conta.Status = StatusConta.LiberadaParaPagamento;
            _db.Aprovacoes.Add(new Aprovacao
            {
                ContaPagarId = conta.Id,
                NivelExigido = nivel,
                Resultado = ResultadoAprovacao.Aprovada,
                Aprovador = "sistema (automatico)",
                DataDecisao = DateTime.UtcNow,
                Observacao = "Aprovacao automatica (abaixo do limite)"
            });
            await _auditoria.RegistrarAsync(AcaoAuditoria.Aprovacao, nameof(ContaPagar), conta.Id,
                valorNovo: "Automatica", usuario: usuario);
        }
        else
        {
            conta.Status = StatusConta.EmAprovacao;
            _db.Aprovacoes.Add(new Aprovacao
            {
                ContaPagarId = conta.Id,
                NivelExigido = nivel,
                Resultado = ResultadoAprovacao.Pendente
            });
        }

        await _db.SaveChangesAsync();

        // Notifica o perfil responsável (nivel = nome da role: Gerente/Diretor).
        if (nivel != NivelAprovacao.Automatica)
            await _notificacoes.NotificarAsync(nivel.ToString(),
                $"Conta aguardando aprovação ({nivel})",
                $"Conta '{conta.Descricao}' de {conta.ValorLiquido:C} precisa de aprovação.",
                SeveridadeNotificacao.Alerta, $"/Aprovacoes");

        return OperationResult.Ok();
    }

    public async Task<OperationResult> AprovarAsync(DecisaoAprovacaoVM vm)
    {
        var (conta, aprovacao, erro) = await CarregarPendente(vm.ContaPagarId);
        if (erro is not null) return OperationResult.Falha(erro);

        aprovacao!.Resultado = ResultadoAprovacao.Aprovada;
        aprovacao.Aprovador = vm.Aprovador;
        aprovacao.DataDecisao = DateTime.UtcNow;
        aprovacao.Observacao = vm.Observacao;
        conta!.Status = StatusConta.LiberadaParaPagamento;

        await _auditoria.RegistrarAsync(AcaoAuditoria.Aprovacao, nameof(ContaPagar), conta.Id,
            valorNovo: vm.Aprovador, usuario: vm.Aprovador);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ReprovarAsync(DecisaoAprovacaoVM vm)
    {
        var (conta, aprovacao, erro) = await CarregarPendente(vm.ContaPagarId);
        if (erro is not null) return OperationResult.Falha(erro);

        aprovacao!.Resultado = ResultadoAprovacao.Reprovada;
        aprovacao.Aprovador = vm.Aprovador;
        aprovacao.DataDecisao = DateTime.UtcNow;
        aprovacao.Observacao = vm.Observacao;
        conta!.Status = StatusConta.Reprovada;

        await _auditoria.RegistrarAsync(AcaoAuditoria.Reprovacao, nameof(ContaPagar), conta.Id,
            valorNovo: vm.Aprovador, usuario: vm.Aprovador);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<List<ContaPagar>> ListarPendentesAsync() =>
        await _db.ContasPagar.AsNoTracking()
            .Include(c => c.Fornecedor)
            .Include(c => c.Aprovacoes)
            .Where(c => c.Status == StatusConta.EmAprovacao)
            .OrderBy(c => c.DataVencimento)
            .ToListAsync();

    private async Task<(ContaPagar? conta, Aprovacao? aprovacao, string? erro)> CarregarPendente(int contaId)
    {
        var conta = await _db.ContasPagar.Include(c => c.Aprovacoes).FirstOrDefaultAsync(c => c.Id == contaId);
        if (conta is null) return (null, null, "Conta nao encontrada.");
        if (conta.Status != StatusConta.EmAprovacao) return (null, null, "Conta nao esta em aprovacao.");

        var aprovacao = conta.Aprovacoes.FirstOrDefault(a => a.Resultado == ResultadoAprovacao.Pendente);
        if (aprovacao is null) return (null, null, "Nao ha aprovacao pendente para esta conta.");

        return (conta, aprovacao, null);
    }
}
