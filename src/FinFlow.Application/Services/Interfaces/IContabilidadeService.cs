using FinFlow.Domain.Entities;
using FinFlow.Helpers;
using FinFlow.ViewModels;

namespace FinFlow.Services.Interfaces;

/// <summary>Códigos das contas contábeis usadas no lançamento automático.</summary>
public static class PlanoContasPadrao
{
    public const string Bancos = "1.1.01";
    public const string Clientes = "1.2.01";
    public const string Fornecedores = "2.1.01";
    public const string Receitas = "3.1.01";
    public const string Despesas = "4.1.01";
}

public interface IContabilidadeService
{
    // Plano de contas
    Task<List<ContaContabil>> ListarPlanoAsync();
    Task<ContaContabil?> ObterContaAsync(int id);
    Task<OperationResult<ContaContabil>> CriarContaAsync(ContaContabil conta);

    // Lançamentos (partida dobrada)
    Task<List<LancamentoContabil>> ListarLancamentosAsync(int take = 100);
    Task<LancamentoContabil?> ObterLancamentoAsync(int id);
    Task<OperationResult<LancamentoContabil>> CriarLancamentoAsync(LancamentoFormVM vm, string usuario, string origem = "Manual");

    // Relatórios
    Task<List<BalanceteLinha>> BalanceteAsync();
    Task<List<RazaoMovimento>> RazaoAsync(int contaContabilId);
    Task<DreResultado> DreAsync();

    // Lançamento automático (integração com o financeiro)
    Task LancarPagamentoAsync(int contaPagarId, decimal valor, string usuario);
    Task LancarRecebimentoAsync(int contaReceberId, decimal valor, string usuario);

    // Estorno automático: gera lançamento de reversão (partidas invertidas).
    Task EstornarPagamentoAsync(int contaPagarId, string usuario);
    Task EstornarRecebimentoAsync(int contaReceberId, string usuario);
}
