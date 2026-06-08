using FinFlow.Domain.Entities;
using FinFlow.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Data;

/// <summary>
/// DbContext do sistema de Contas a Pagar. Tambem hospeda as tabelas do Identity
/// (usuarios/roles). Funciona com Oracle, SQL Server ou InMemory.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Empresa (tenant) usada para filtrar as consultas. Definida por request pelo
    /// TenantMiddleware (a partir do claim do usuário). Null = sem filtro (seed/jobs/admin global).
    /// </summary>
    public int? EmpresaIdFiltro { get; set; }

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<CentroCusto> CentrosCusto => Set<CentroCusto>();
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
    public DbSet<RetencaoImposto> Retencoes => Set<RetencaoImposto>();
    public DbSet<Aprovacao> Aprovacoes => Set<Aprovacao>();
    public DbSet<BaixaPagamento> Baixas => Set<BaixaPagamento>();
    public DbSet<BankTransaction> Transacoes => Set<BankTransaction>();
    public DbSet<ExtratoBancarioItem> ExtratoItens => Set<ExtratoBancarioItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Contas a Receber (Fase 6)
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();
    public DbSet<RecebimentoBaixa> Recebimentos => Set<RecebimentoBaixa>();

    // Workflow de aprovação configurável (Fase 3)
    public DbSet<RegraAprovacao> RegrasAprovacao => Set<RegraAprovacao>();

    // Anexos (Fase 9)
    public DbSet<Anexo> Anexos => Set<Anexo>();

    // Notificações (Fase 12)
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    // Webhooks bancários (Fase 4)
    public DbSet<BankWebhookEvent> BankWebhookEvents => Set<BankWebhookEvent>();

    // Compras (Fase 8)
    public DbSet<SolicitacaoCompra> SolicitacoesCompra => Set<SolicitacaoCompra>();

    // Chat interno (Módulo 24)
    public DbSet<ChatConversation> Conversas => Set<ChatConversation>();
    public DbSet<ChatParticipant> ChatParticipantes => Set<ChatParticipant>();
    public DbSet<ChatMessage> ChatMensagens => Set<ChatMessage>();
    public DbSet<ChatAttachment> ChatAnexos => Set<ChatAttachment>();
    public DbSet<ChatReadReceipt> ChatLeituras => Set<ChatReadReceipt>();
    public DbSet<ChatMention> ChatMencoes => Set<ChatMention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica configuracoes (IEntityTypeConfiguration) deste assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Precisao padrao para TODAS as colunas decimais (dinheiro): 18,2.
        // Evita warning do EF e perda de precisao monetaria.
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Multitenant (Fase 14): filtro global por empresa nas entidades tenant.
        // EmpresaIdFiltro nulo (seed/jobs) => sem filtro.
        AplicarFiltroTenant<Empresa>(modelBuilder, soByPropriaEmpresa: true);
        AplicarFiltroTenant<Fornecedor>(modelBuilder);
        AplicarFiltroTenant<Cliente>(modelBuilder);
        AplicarFiltroTenant<Categoria>(modelBuilder);
        AplicarFiltroTenant<CentroCusto>(modelBuilder);
        AplicarFiltroTenant<ContaBancaria>(modelBuilder);
        AplicarFiltroTenant<ContaPagar>(modelBuilder);
        AplicarFiltroTenant<ContaReceber>(modelBuilder);
        AplicarFiltroTenant<RegraAprovacao>(modelBuilder);
        AplicarFiltroTenant<SolicitacaoCompra>(modelBuilder);
        AplicarFiltroTenant<ChatConversation>(modelBuilder);
        AplicarFiltroTenant<AuditLog>(modelBuilder);
    }

    private void AplicarFiltroTenant<T>(ModelBuilder mb, bool soByPropriaEmpresa = false) where T : class
    {
        if (soByPropriaEmpresa)
            mb.Entity<Empresa>().HasQueryFilter(e => EmpresaIdFiltro == null || e.Id == EmpresaIdFiltro);
        else
            mb.Entity<T>().HasQueryFilter(BuildTenantFilter<T>());
    }

    private System.Linq.Expressions.Expression<Func<T, bool>> BuildTenantFilter<T>() where T : class
    {
        // e => EmpresaIdFiltro == null || ((ITenantOwned)e).EmpresaId == EmpresaIdFiltro
        var p = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
        var ctx = System.Linq.Expressions.Expression.Constant(this);
        var filtro = System.Linq.Expressions.Expression.Property(ctx, nameof(EmpresaIdFiltro));
        var empresaId = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Convert(p, typeof(ITenantOwned)), nameof(ITenantOwned.EmpresaId));
        var semFiltro = System.Linq.Expressions.Expression.Equal(filtro,
            System.Linq.Expressions.Expression.Constant(null, typeof(int?)));
        var igual = System.Linq.Expressions.Expression.Equal(
            System.Linq.Expressions.Expression.Convert(empresaId, typeof(int?)), filtro);
        var corpo = System.Linq.Expressions.Expression.OrElse(semFiltro, igual);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(corpo, p);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Filtro de tenant no agregado (ContaPagar) sem filtro nos dependentes é intencional.
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    /// <summary>Mantem AtualizadoEm preenchido automaticamente nas alteracoes.</summary>
    public override int SaveChanges()
    {
        AplicarTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicarTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = DateTime.UtcNow;
                // Carimba a empresa atual em novas entidades tenant.
                if (EmpresaIdFiltro.HasValue && entry.Entity is ITenantOwned tenant)
                    tenant.EmpresaId = EmpresaIdFiltro.Value;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = DateTime.UtcNow;
            }
        }
    }
}
