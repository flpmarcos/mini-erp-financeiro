using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FinFlow.Tests.Integration;

/// <summary>
/// Sobe a aplicacao real (WebApplicationFactory&lt;Program&gt;) apontando o EF Core
/// para o Oracle efemero do container. Exercita o pipeline completo: DI, seed,
/// migrations/EnsureCreated, controllers e views.
/// </summary>
public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebAppFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production"); // evita exceptions detalhadas; testa pipeline real
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Oracle",
                ["ConnectionStrings:Default"] = _connectionString,
                ["Jobs:Enabled"] = "false"
            });
        });
    }
}
