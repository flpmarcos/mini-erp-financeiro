using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Integrations.Banking;
using FinFlow.Services.Interfaces;
using FinFlow.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Baixa de contas. Aplica as regras de pagamento, calcula encargos de atraso,
/// chama a integracao bancaria simulada e registra a transacao + auditoria.
/// </summary>
public class PagamentoService : IPagamentoService
{
    private readonly AppDbContext _db;
    private readonly IJurosMultaService _juros;
    private readonly IBankPaymentServiceFactory _bankFactory;
    private readonly IAuditoriaService _auditoria;

    public PagamentoService(AppDbContext db, IJurosMultaService juros,
        IBankPaymentServiceFactory bankFactory, IAuditoriaService auditoria)
    {
        _db = db;
        _juros = juros;
        _bankFactory = bankFactory;
        _auditoria = auditoria;
    }

    public async Task<OperationResult> BaixarAsync(BaixaVM vm, string usuario)
    {
        var conta = await _db.ContasPagar
            .Include(c => c.Fornecedor)
            .FirstOrDefaultAsync(c => c.Id == vm.ContaPagarId);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");

        // ---- Regras de bloqueio ----
        if (conta.Status == StatusConta.Cancelada) return OperationResult.Falha("Conta cancelada nao pode ser paga.");
        if (conta.Status == StatusConta.Reprovada) return OperationResult.Falha("Conta reprovada nao pode ser paga.");
        if (conta.Status == StatusConta.Paga) return OperationResult.Falha("Conta ja esta paga.");
        if (conta.Fornecedor is { PodeReceberPagamento: false })
            return OperationResult.Falha("Fornecedor bloqueado/inativo nao pode receber pagamento.");
        if (vm.ValorPago <= 0) return OperationResult.Falha("Valor pago deve ser maior que zero.");

        var conta_bancaria = await _db.ContasBancarias.FindAsync(vm.ContaBancariaId);
        if (conta_bancaria is null) return OperationResult.Falha("Conta bancaria invalida.");

        // ---- Encargos por atraso (informativo) ----
        var encargos = _juros.Calcular(conta);
        var saldoDevedor = conta.SaldoDevedor;

        // Valor pago maior que o devido exige justificativa.
        if (vm.ValorPago > saldoDevedor && string.IsNullOrWhiteSpace(vm.Justificativa))
            return OperationResult.Falha("Valor pago maior que o devido exige justificativa.");

        // ---- Integracao bancaria simulada ----
        var bank = _bankFactory.Resolver(conta_bancaria.BancoIntegracao);
        var resp = await bank.PagarAsync(new BankPaymentRequest
        {
            ContaPagarId = conta.Id,
            Valor = vm.ValorPago,
            Forma = vm.FormaPagamento,
            Favorecido = conta.Fornecedor?.RazaoSocial ?? "N/D",
            ChavePix = conta.ChavePix,
            CodigoBarras = conta.CodigoBarras,
            Documento = conta.Fornecedor?.Documento
        });

        var transacao = new BankTransaction
        {
            ContaPagarId = conta.Id,
            Banco = conta_bancaria.BancoIntegracao,
            TipoPagamento = vm.FormaPagamento,
            Status = resp.Status,
            CodigoTransacao = resp.CodigoTransacao,
            Valor = vm.ValorPago,
            PayloadEnvio = resp.PayloadEnvio,
            PayloadResposta = resp.PayloadResposta,
            DataRetorno = DateTime.UtcNow,
            MensagemErro = resp.MensagemErro
        };
        _db.Transacoes.Add(transacao);

        // Banco recusou: registra transacao com erro mas nao baixa a conta.
        if (resp.Status == StatusTransacaoBancaria.Erro)
        {
            await _db.SaveChangesAsync();
            return OperationResult.Falha($"Pagamento rejeitado pelo banco: {resp.MensagemErro}");
        }

        // ---- Registra a baixa ----
        _db.Baixas.Add(new BaixaPagamento
        {
            ContaPagarId = conta.Id,
            DataPagamento = vm.DataPagamento,
            ValorPago = vm.ValorPago,
            Encargos = encargos.DiasAtraso > 0 ? encargos.Multa + encargos.Juros : 0m,
            ContaBancariaId = vm.ContaBancariaId,
            FormaPagamento = vm.FormaPagamento,
            Comprovante = vm.Comprovante,
            Observacao = vm.Observacao,
            Justificativa = vm.Justificativa
        });

        conta.ValorPago += vm.ValorPago;
        conta.DataPagamento = vm.DataPagamento;

        // ---- Define novo status conforme o valor pago ----
        if (conta.ValorPago >= conta.ValorLiquido)
            conta.Status = StatusConta.Paga;
        else
            conta.Status = StatusConta.ParcialmentePaga;

        await _auditoria.RegistrarAsync(AcaoAuditoria.Pagamento, nameof(ContaPagar), conta.Id,
            "ValorPago", saldoDevedor.ToString("F2"), vm.ValorPago.ToString("F2"), usuario);

        await _db.SaveChangesAsync();

        return resp.Status == StatusTransacaoBancaria.Pendente
            ? OperationResult.Falha("Pagamento registrado, porem o banco retornou PENDENTE (em processamento). Acompanhe a transacao.")
            : OperationResult.Ok();
    }
}
