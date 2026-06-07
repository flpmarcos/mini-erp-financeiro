using ContasAPagar.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContasAPagar.Web.Data.Configurations;

public class EmpresaConfig : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("EMPRESAS");
        b.Property(x => x.RazaoSocial).IsRequired().HasMaxLength(200);
    }
}

public class FornecedorConfig : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> b)
    {
        b.ToTable("FORNECEDORES");
        b.HasKey(x => x.Id);
        b.Property(x => x.RazaoSocial).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.Documento).IsUnique();
    }
}

public class ContaPagarConfig : IEntityTypeConfiguration<ContaPagar>
{
    public void Configure(EntityTypeBuilder<ContaPagar> b)
    {
        b.ToTable("CONTAS_PAGAR");
        b.HasKey(x => x.Id);

        // FKs obrigatorias - Restrict evita apagar cadastro com contas vinculadas.
        b.HasOne(x => x.Fornecedor).WithMany(f => f.Contas)
            .HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Categoria).WithMany(c => c.Contas)
            .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CentroCusto).WithMany(c => c.Contas)
            .HasForeignKey(x => x.CentroCustoId).OnDelete(DeleteBehavior.Restrict);

        // Auto-relacionamento: parcelas apontam para a conta origem.
        b.HasOne(x => x.ContaOrigem).WithMany(x => x.Parcelas)
            .HasForeignKey(x => x.ContaOrigemId).OnDelete(DeleteBehavior.Restrict);

        // Indices para filtros/relatorios mais usados.
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.DataVencimento);
        b.HasIndex(x => x.FornecedorId);
    }
}

public class RetencaoImpostoConfig : IEntityTypeConfiguration<RetencaoImposto>
{
    public void Configure(EntityTypeBuilder<RetencaoImposto> b)
    {
        b.ToTable("RETENCOES_IMPOSTO");
        b.HasOne(x => x.ContaPagar).WithMany(c => c.Retencoes)
            .HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AprovacaoConfig : IEntityTypeConfiguration<Aprovacao>
{
    public void Configure(EntityTypeBuilder<Aprovacao> b)
    {
        b.ToTable("APROVACOES");
        b.HasOne(x => x.ContaPagar).WithMany(c => c.Aprovacoes)
            .HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BaixaPagamentoConfig : IEntityTypeConfiguration<BaixaPagamento>
{
    public void Configure(EntityTypeBuilder<BaixaPagamento> b)
    {
        b.ToTable("BAIXAS_PAGAMENTO");
        b.HasOne(x => x.ContaPagar).WithMany(c => c.Baixas)
            .HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ContaBancaria).WithMany()
            .HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankTransactionConfig : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> b)
    {
        b.ToTable("BANK_TRANSACTIONS");
        b.HasOne(x => x.ContaPagar).WithMany(c => c.Transacoes)
            .HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExtratoConfig : IEntityTypeConfiguration<ExtratoBancarioItem>
{
    public void Configure(EntityTypeBuilder<ExtratoBancarioItem> b)
    {
        b.ToTable("EXTRATO_BANCARIO");
        b.HasOne(x => x.ContaPagar).WithMany()
            .HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("CLIENTES");
        b.Property(x => x.RazaoSocial).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.Documento).IsUnique();
    }
}

public class ContaReceberConfig : IEntityTypeConfiguration<ContaReceber>
{
    public void Configure(EntityTypeBuilder<ContaReceber> b)
    {
        b.ToTable("CONTAS_RECEBER");
        b.HasOne(x => x.Cliente).WithMany(c => c.Contas)
            .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Categoria).WithMany()
            .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.DataVencimento);
    }
}

public class RecebimentoBaixaConfig : IEntityTypeConfiguration<RecebimentoBaixa>
{
    public void Configure(EntityTypeBuilder<RecebimentoBaixa> b)
    {
        b.ToTable("RECEBIMENTOS");
        b.HasOne(x => x.ContaReceber).WithMany(c => c.Recebimentos)
            .HasForeignKey(x => x.ContaReceberId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ContaBancaria).WithMany()
            .HasForeignKey(x => x.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RegraAprovacaoConfig : IEntityTypeConfiguration<RegraAprovacao>
{
    public void Configure(EntityTypeBuilder<RegraAprovacao> b)
    {
        b.ToTable("REGRAS_APROVACAO");
        b.Ignore(x => x.Especificidade);
        b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.CentroCusto).WithMany().HasForeignKey(x => x.CentroCustoId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AnexoConfig : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> b)
    {
        b.ToTable("ANEXOS");
        b.HasOne(x => x.ContaPagar).WithMany().HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificacaoConfig : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> b)
    {
        b.ToTable("NOTIFICACOES");
        b.HasIndex(x => new { x.Destinatario, x.Lida });
    }
}

public class BankWebhookEventConfig : IEntityTypeConfiguration<BankWebhookEvent>
{
    public void Configure(EntityTypeBuilder<BankWebhookEvent> b)
    {
        b.ToTable("BANK_WEBHOOK_EVENTS");
        b.HasIndex(x => x.CodigoTransacao);
    }
}

public class SolicitacaoCompraConfig : IEntityTypeConfiguration<SolicitacaoCompra>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCompra> b)
    {
        b.ToTable("SOLICITACOES_COMPRA");
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CentroCusto).WithMany().HasForeignKey(x => x.CentroCustoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ContaPagarGerada).WithMany().HasForeignKey(x => x.ContaPagarGeradaId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.Status);
    }
}

public class ChatConversationConfig : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> b)
    {
        b.ToTable("CHAT_CONVERSAS");
        b.Ignore(x => x.EhAuditavel);
        b.HasOne(x => x.ContaPagar).WithMany().HasForeignKey(x => x.ContaPagarId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.SolicitacaoCompra).WithMany().HasForeignKey(x => x.SolicitacaoCompraId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.ContaPagarId);
    }
}

public class ChatParticipantConfig : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> b)
    {
        b.ToTable("CHAT_PARTICIPANTES");
        b.HasOne(x => x.Conversation).WithMany(c => c.Participantes).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ConversationId, x.Usuario }).IsUnique();
    }
}

public class ChatMessageConfig : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("CHAT_MENSAGENS");
        b.HasOne(x => x.Conversation).WithMany(c => c.Mensagens).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.ConversationId);
    }
}

public class ChatAttachmentConfig : IEntityTypeConfiguration<ChatAttachment>
{
    public void Configure(EntityTypeBuilder<ChatAttachment> b)
    {
        b.ToTable("CHAT_ANEXOS");
        b.HasOne(x => x.Message).WithMany(m => m.Anexos).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatReadReceiptConfig : IEntityTypeConfiguration<ChatReadReceipt>
{
    public void Configure(EntityTypeBuilder<ChatReadReceipt> b)
    {
        b.ToTable("CHAT_LEITURAS");
        b.HasOne(x => x.Message).WithMany(m => m.Leituras).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.MessageId, x.Usuario }).IsUnique();
    }
}

public class ChatMentionConfig : IEntityTypeConfiguration<ChatMention>
{
    public void Configure(EntityTypeBuilder<ChatMention> b)
    {
        b.ToTable("CHAT_MENCOES");
        b.HasOne(x => x.Message).WithMany(m => m.Mencoes).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AUDIT_LOGS");
        b.HasIndex(x => new { x.Entidade, x.EntidadeId });
    }
}
