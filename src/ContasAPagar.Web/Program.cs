using ContasAPagar.Web.Configurations;
using ContasAPagar.Web.Data;
using ContasAPagar.Web.Infrastructure.Observability;
using ContasAPagar.Web.Integrations.Banking;
using ContasAPagar.Web.Repositories;
using ContasAPagar.Web.Services;
using ContasAPagar.Web.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// Serilog - logs estruturados (console + arquivo rotativo). CorrelationId vem
// do middleware. Em producao troque por sink central (Seq, Elastic, etc).
// ----------------------------------------------------------------------------
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{CorrelationId}>{NewLine}{Exception}")
    .WriteTo.File("logs/contas-a-pagar-.log", rollingInterval: RollingInterval.Day,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <{CorrelationId}>{NewLine}{Exception}"));

// ----------------------------------------------------------------------------
// Configuracao (Options pattern) - parametros financeiros vem do appsettings.
// ----------------------------------------------------------------------------
builder.Services.Configure<FinanceiroOptions>(
    builder.Configuration.GetSection(FinanceiroOptions.SectionName));

// ----------------------------------------------------------------------------
// Banco de dados - provider escolhido via "Database:Provider":
//   "Oracle"    -> Oracle (primeira opcao, producao/estudo realista)
//   "SqlServer" -> SQL Server (segunda opcao)
//   "InMemory"  -> roda SEM banco (default; ideal para estudo rapido e testes)
// ----------------------------------------------------------------------------
var provider = builder.Configuration["Database:Provider"] ?? "InMemory";
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider.ToLowerInvariant())
    {
        case "oracle":
            options.UseOracle(connectionString);
            break;
        case "sqlserver":
            options.UseSqlServer(connectionString);
            break;
        default:
            options.UseInMemoryDatabase("ContasAPagarDb");
            break;
    }
});

// ----------------------------------------------------------------------------
// Injecao de dependencia - Repository, Services e integracoes bancarias.
// ----------------------------------------------------------------------------
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IJurosMultaService, JurosMultaService>();
builder.Services.AddScoped<IFornecedorService, FornecedorService>();
builder.Services.AddScoped<ICadastroService, CadastroService>();
builder.Services.AddScoped<IContaPagarService, ContaPagarService>();
builder.Services.AddScoped<IAprovacaoService, AprovacaoService>();
builder.Services.AddScoped<IRegraAprovacaoService, RegraAprovacaoService>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddScoped<IConciliacaoService, ConciliacaoService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Contas a Receber (Fase 6)
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IContaReceberService, ContaReceberService>();

// Fluxo de Caixa (Fase 7)
builder.Services.AddScoped<IFluxoCaixaService, FluxoCaixaService>();

// Anexos (Fase 9) - storage local; troque por S3/MinIO implementando IFileStorage.
var uploadDir = builder.Configuration["Storage:LocalPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "uploads");
builder.Services.AddSingleton<ContasAPagar.Web.Integrations.Storage.IFileStorage>(
    new ContasAPagar.Web.Integrations.Storage.LocalFileStorage(uploadDir));
builder.Services.AddScoped<IAnexoService, AnexoService>();

// CNAB fake (Fase 5)
builder.Services.AddScoped<ICnabService, CnabService>();

// Notificações (Fase 12) - internas + canais fake (e-mail/WhatsApp)
builder.Services.AddScoped<ContasAPagar.Web.Integrations.Notifications.INotificationChannel,
    ContasAPagar.Web.Integrations.Notifications.FakeEmailChannel>();
builder.Services.AddScoped<ContasAPagar.Web.Integrations.Notifications.INotificationChannel,
    ContasAPagar.Web.Integrations.Notifications.FakeWhatsappChannel>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();

// Compras (Fase 8)
builder.Services.AddScoped<ICompraService, CompraService>();

// Chat interno (Módulo 24)
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSignalR();

// Assistente RAG (Módulo 25) — providers FAKE/local (sem custo). Troque por OpenAI/Azure + Qdrant/pgvector (ver README).
builder.Services.AddSingleton<ContasAPagar.Web.Integrations.Rag.IEmbeddingService, ContasAPagar.Web.Integrations.Rag.FakeEmbeddingService>();
builder.Services.AddSingleton<ContasAPagar.Web.Integrations.Rag.IVectorStore, ContasAPagar.Web.Integrations.Rag.InMemoryVectorStore>();
builder.Services.AddSingleton<ContasAPagar.Web.Integrations.Rag.ILlmProvider, ContasAPagar.Web.Integrations.Rag.FakeLlmProvider>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IPermissionAwareRetriever, PermissionAwareRetriever>();
builder.Services.AddScoped<IRagService, RagService>();

// Bancos fake: todos implementam IBankPaymentService; a factory escolhe pelo banco.
builder.Services.AddScoped<IBankPaymentService, GenericoPaymentServiceFake>();
builder.Services.AddScoped<IBankPaymentService, BancoBrasilPaymentServiceFake>();
builder.Services.AddScoped<IBankPaymentService, ItauPaymentServiceFake>();
builder.Services.AddScoped<IBankPaymentService, SantanderPaymentServiceFake>();
builder.Services.AddScoped<IBankPaymentServiceFactory, BankPaymentServiceFactory>();
builder.Services.AddScoped<IBankIntegrationService, BankIntegrationService>();

// ----------------------------------------------------------------------------
// Identity (autenticacao por cookie) + perfis (roles).
// ----------------------------------------------------------------------------
builder.Services
    .AddIdentity<ContasAPagar.Web.Domain.Identity.AppUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        // Politica de senha relaxada para estudo (endureca em producao).
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<ContasAPagar.Web.Infrastructure.Tenancy.TenantClaimsFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Policies por perfil (usadas com [Authorize(Policy = ...)]).
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ContasAPagar.Web.Domain.Identity.Policies.PodeCadastrar, p => p.RequireRole(
        ContasAPagar.Web.Domain.Identity.Roles.Admin, ContasAPagar.Web.Domain.Identity.Roles.Financeiro))
    .AddPolicy(ContasAPagar.Web.Domain.Identity.Policies.PodePagar, p => p.RequireRole(
        ContasAPagar.Web.Domain.Identity.Roles.Admin, ContasAPagar.Web.Domain.Identity.Roles.Financeiro))
    .AddPolicy(ContasAPagar.Web.Domain.Identity.Policies.PodeAprovar, p => p.RequireRole(
        ContasAPagar.Web.Domain.Identity.Roles.Admin, ContasAPagar.Web.Domain.Identity.Roles.Gerente,
        ContasAPagar.Web.Domain.Identity.Roles.Diretor))
    .AddPolicy(ContasAPagar.Web.Domain.Identity.Policies.PodeVisualizar, p => p.RequireAuthenticatedUser())
    .AddPolicy(ContasAPagar.Web.Domain.Identity.Policies.Administrar, p => p.RequireRole(
        ContasAPagar.Web.Domain.Identity.Roles.Admin));

// Health checks: liveness simples + readiness do banco (quando relacional).
var healthChecks = builder.Services.AddHealthChecks();
if (!provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    healthChecks.AddDbContextCheck<AppDbContext>("database");

// ----------------------------------------------------------------------------
// Jobs em background (Hangfire). Desligável via Jobs:Enabled (testes desligam).
// Storage em memória — para estudo. Em produção use SQL Server/PostgreSQL/Redis.
// ----------------------------------------------------------------------------
var jobsEnabled = builder.Configuration.GetValue("Jobs:Enabled", true);
if (jobsEnabled)
{
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage());
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<ContasAPagar.Web.Infrastructure.Jobs.JobsFinanceiros>();
}

builder.Services.AddControllersWithViews();

// API auxiliar + documentacao Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Contas a Pagar API", Version = "v1",
        Description = "API REST auxiliar (read-only) do sistema de Contas a Pagar / Receber / Fluxo de Caixa." }));

var app = builder.Build();

// ----------------------------------------------------------------------------
// Inicializacao do banco + seed de dados de teste.
// ----------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.IsRelational())
    {
        // Aplica migrations se existirem; caso contrario cria o schema direto.
        if (db.Database.GetMigrations().Any())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();
    }
    else
    {
        db.Database.EnsureCreated(); // InMemory
    }

    await DataSeeder.SeedAsync(db);
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);

    // Indexa a base de conhecimento para o assistente RAG (vector store em memória).
    await scope.ServiceProvider.GetRequiredService<IDocumentIngestionService>().IngerirBaseAsync();
}

// ----------------------------------------------------------------------------
// Pipeline HTTP.
// ----------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Swagger UI em /swagger (habilitado tambem fora de Dev para fins de estudo).
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contas a Pagar API v1"));

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();   // 1 log resumido por request (metodo, rota, status, tempo)

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ContasAPagar.Web.Infrastructure.Tenancy.TenantMiddleware>(); // define empresa atual

// Endpoint de health (JSON simples). Pagina amigavel fica em /Status.
app.MapHealthChecks("/health").WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

// Painel e jobs recorrentes do Hangfire.
if (jobsEnabled)
{
    app.MapHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new ContasAPagar.Web.Infrastructure.Jobs.HangfireDashboardAuth() }
    });

    Hangfire.RecurringJob.AddOrUpdate<ContasAPagar.Web.Infrastructure.Jobs.JobsFinanceiros>(
        "atualizar-vencidas", j => j.AtualizarVencidasAsync(), Hangfire.Cron.Hourly);
    Hangfire.RecurringJob.AddOrUpdate<ContasAPagar.Web.Infrastructure.Jobs.JobsFinanceiros>(
        "alertas-vencimento", j => j.EnviarAlertasVencimentoAsync(), Hangfire.Cron.Daily);
    Hangfire.RecurringJob.AddOrUpdate<ContasAPagar.Web.Infrastructure.Jobs.JobsFinanceiros>(
        "retry-transacoes-bancarias", j => j.ReprocessarTransacoesPendentesAsync(), Hangfire.Cron.Hourly);
}

app.MapControllers(); // controllers de API com rotas por atributo (api/v1/*)
app.MapHub<ContasAPagar.Web.Hubs.ChatHub>("/chatHub"); // SignalR do chat interno

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

// Expoe Program para testes de integracao (WebApplicationFactory<Program>).
public partial class Program
{
    /// <summary>Momento de inicializacao do processo (para exibir uptime em /Status).</summary>
    public static readonly DateTime StartedAtUtc = DateTime.UtcNow;
}
