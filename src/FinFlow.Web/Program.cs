using FinFlow.Configurations;
using FinFlow.Data;
using FinFlow.Infrastructure.Observability;
using FinFlow.Integrations.Banking;
using FinFlow.Repositories;
using FinFlow.Services;
using FinFlow.Services.Interfaces;
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
    .WriteTo.File("logs/finflow-.log", rollingInterval: RollingInterval.Day,
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
            options.UseInMemoryDatabase("FinFlowDb");
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
builder.Services.AddSingleton<FinFlow.Integrations.Storage.IFileStorage>(
    new FinFlow.Integrations.Storage.LocalFileStorage(uploadDir));
builder.Services.AddScoped<IAnexoService, AnexoService>();

// CNAB fake (Fase 5)
builder.Services.AddScoped<ICnabService, CnabService>();

// Notificações (Fase 12) - internas + canais fake (e-mail/WhatsApp)
builder.Services.AddScoped<FinFlow.Integrations.Notifications.INotificationChannel,
    FinFlow.Integrations.Notifications.FakeEmailChannel>();
builder.Services.AddScoped<FinFlow.Integrations.Notifications.INotificationChannel,
    FinFlow.Integrations.Notifications.FakeWhatsappChannel>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();

// Compras (Fase 8)
builder.Services.AddScoped<ICompraService, CompraService>();

// Chat interno (Módulo 24)
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSignalR();

// Assistente RAG (Módulo 25) — providers FAKE/local (sem custo). Troque por OpenAI/Azure + Qdrant/pgvector (ver README).
builder.Services.AddSingleton<FinFlow.Integrations.Rag.IEmbeddingService, FinFlow.Integrations.Rag.FakeEmbeddingService>();
builder.Services.AddSingleton<FinFlow.Integrations.Rag.IVectorStore, FinFlow.Integrations.Rag.InMemoryVectorStore>();
builder.Services.AddSingleton<FinFlow.Integrations.Rag.ILlmProvider, FinFlow.Integrations.Rag.FakeLlmProvider>();
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
    .AddIdentity<FinFlow.Domain.Identity.AppUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        // Politica de senha relaxada para estudo (endureca em producao).
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<FinFlow.Infrastructure.Tenancy.TenantClaimsFactory>()
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
    .AddPolicy(FinFlow.Domain.Identity.Policies.PodeCadastrar, p => p.RequireRole(
        FinFlow.Domain.Identity.Roles.Admin, FinFlow.Domain.Identity.Roles.Financeiro))
    .AddPolicy(FinFlow.Domain.Identity.Policies.PodePagar, p => p.RequireRole(
        FinFlow.Domain.Identity.Roles.Admin, FinFlow.Domain.Identity.Roles.Financeiro))
    .AddPolicy(FinFlow.Domain.Identity.Policies.PodeAprovar, p => p.RequireRole(
        FinFlow.Domain.Identity.Roles.Admin, FinFlow.Domain.Identity.Roles.Gerente,
        FinFlow.Domain.Identity.Roles.Diretor))
    .AddPolicy(FinFlow.Domain.Identity.Policies.PodeVisualizar, p => p.RequireAuthenticatedUser())
    .AddPolicy(FinFlow.Domain.Identity.Policies.Administrar, p => p.RequireRole(
        FinFlow.Domain.Identity.Roles.Admin));

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
    builder.Services.AddScoped<FinFlow.Infrastructure.Jobs.JobsFinanceiros>();
}

builder.Services.AddControllersWithViews();

// API auxiliar + documentacao Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "FinFlow API", Version = "v1",
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

// Atrás de um proxy reverso (Coolify/Traefik, Nginx...) que termina o TLS:
// confia nos X-Forwarded-Proto/For para o app saber que a requisição é HTTPS.
if (!app.Environment.IsDevelopment())
{
    var fwd = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    };
    fwd.KnownNetworks.Clear();
    fwd.KnownProxies.Clear();
    app.UseForwardedHeaders(fwd);

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Swagger UI em /swagger (habilitado tambem fora de Dev para fins de estudo).
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinFlow API v1"));

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();   // 1 log resumido por request (metodo, rota, status, tempo)

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FinFlow.Infrastructure.Tenancy.TenantMiddleware>(); // define empresa atual

// Endpoint de health (JSON simples). Pagina amigavel fica em /Status.
app.MapHealthChecks("/health").WithMetadata(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());

// Painel e jobs recorrentes do Hangfire.
if (jobsEnabled)
{
    app.MapHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new FinFlow.Infrastructure.Jobs.HangfireDashboardAuth() }
    });

    Hangfire.RecurringJob.AddOrUpdate<FinFlow.Infrastructure.Jobs.JobsFinanceiros>(
        "atualizar-vencidas", j => j.AtualizarVencidasAsync(), Hangfire.Cron.Hourly);
    Hangfire.RecurringJob.AddOrUpdate<FinFlow.Infrastructure.Jobs.JobsFinanceiros>(
        "alertas-vencimento", j => j.EnviarAlertasVencimentoAsync(), Hangfire.Cron.Daily);
    Hangfire.RecurringJob.AddOrUpdate<FinFlow.Infrastructure.Jobs.JobsFinanceiros>(
        "retry-transacoes-bancarias", j => j.ReprocessarTransacoesPendentesAsync(), Hangfire.Cron.Hourly);
}

app.MapControllers(); // controllers de API com rotas por atributo (api/v1/*)
app.MapHub<FinFlow.Hubs.ChatHub>("/chatHub"); // SignalR do chat interno

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
