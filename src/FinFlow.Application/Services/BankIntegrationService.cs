using FinFlow.Data;
using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using FinFlow.Helpers;
using FinFlow.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Services;

/// <summary>
/// Operações avançadas de integração bancária (Fase 4): estorno, processamento de
/// webhook (confirmação assíncrona) e retry de transações pendentes.
/// </summary>
public class BankIntegrationService : IBankIntegrationService
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;
    private readonly INotificacaoService _notificacoes;

    public BankIntegrationService(AppDbContext db, IAuditoriaService auditoria, INotificacaoService notificacoes)
    {
        _db = db;
        _auditoria = auditoria;
        _notificacoes = notificacoes;
    }

    public async Task<OperationResult> EstornarAsync(int contaPagarId, string motivo, string usuario)
    {
        if (string.IsNullOrWhiteSpace(motivo)) return OperationResult.Falha("Informe o motivo do estorno.");

        var conta = await _db.ContasPagar.FirstOrDefaultAsync(c => c.Id == contaPagarId);
        if (conta is null) return OperationResult.Falha("Conta nao encontrada.");
        if (conta.Status is not (StatusConta.Paga or StatusConta.ParcialmentePaga))
            return OperationResult.Falha("Apenas conta paga (ou parcial) pode ser estornada.");

        var valorEstornado = conta.ValorPago;
        conta.ValorPago = 0m;
        conta.DataPagamento = null;
        conta.Status = StatusConta.Estornada;

        _db.Transacoes.Add(new BankTransaction
        {
            ContaPagarId = conta.Id,
            Banco = BancoIntegracao.Generico,
            TipoPagamento = conta.FormaPagamento,
            Status = StatusTransacaoBancaria.Estornado,
            Valor = valorEstornado,
            CodigoTransacao = $"EST{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            PayloadResposta = "{ \"status\": \"ESTORNADO\" }",
            DataRetorno = DateTime.UtcNow,
            MensagemErro = motivo
        });

        await _auditoria.RegistrarAsync(AcaoAuditoria.Estorno, nameof(ContaPagar), conta.Id,
            "ValorPago", valorEstornado.ToString("F2"), "0.00", usuario, motivo);
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> ProcessarWebhookAsync(string codigoTransacao, StatusTransacaoBancaria status, string? payload, string usuario)
    {
        var evento = new BankWebhookEvent
        {
            CodigoTransacao = codigoTransacao,
            StatusInformado = status,
            Payload = payload
        };
        _db.BankWebhookEvents.Add(evento);

        var transacao = await _db.Transacoes
            .Where(t => t.CodigoTransacao == codigoTransacao)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        if (transacao is null)
        {
            evento.Processado = true;
            evento.Resultado = "Transacao nao encontrada.";
            await _db.SaveChangesAsync();
            return OperationResult.Falha("Transacao nao encontrada para o codigo informado.");
        }

        transacao.Status = status;
        transacao.DataRetorno = DateTime.UtcNow;

        var conta = await _db.ContasPagar.FirstOrDefaultAsync(c => c.Id == transacao.ContaPagarId);
        if (conta is not null && status == StatusTransacaoBancaria.Sucesso
            && conta.Status is not (StatusConta.Paga or StatusConta.Cancelada))
        {
            conta.ValorPago += transacao.Valor;
            conta.DataPagamento = DateTime.Today;
            conta.Status = conta.ValorPago >= conta.ValorLiquido ? StatusConta.Paga : StatusConta.ParcialmentePaga;
            await _notificacoes.NotificarAsync(Domain.Identity.Roles.Financeiro,
                "Pagamento confirmado (webhook)", $"Conta '{conta.Descricao}' confirmada pelo banco.",
                SeveridadeNotificacao.Sucesso, $"/Contas/Details/{conta.Id}");
        }

        evento.Processado = true;
        evento.Resultado = $"Transacao {transacao.Id} atualizada para {status}.";
        await _auditoria.RegistrarAsync(AcaoAuditoria.Pagamento, nameof(BankTransaction), transacao.Id,
            "Status", null, status.ToString(), usuario, "Webhook bancario");
        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<int> ReprocessarPendentesAsync(string usuario)
    {
        var pendentes = await _db.Transacoes.Where(t => t.Status == StatusTransacaoBancaria.Pendente).ToListAsync();
        int confirmadas = 0;

        foreach (var t in pendentes)
        {
            // Consulta de status simulada: pendentes antigas são confirmadas.
            t.Status = StatusTransacaoBancaria.Sucesso;
            t.DataRetorno = DateTime.UtcNow;

            var conta = await _db.ContasPagar.FirstOrDefaultAsync(c => c.Id == t.ContaPagarId);
            if (conta is not null && conta.Status is not (StatusConta.Paga or StatusConta.Cancelada))
            {
                conta.ValorPago += t.Valor;
                conta.DataPagamento = DateTime.Today;
                conta.Status = conta.ValorPago >= conta.ValorLiquido ? StatusConta.Paga : StatusConta.ParcialmentePaga;
            }
            confirmadas++;
        }

        if (pendentes.Count > 0) await _db.SaveChangesAsync();
        return confirmadas;
    }
}
