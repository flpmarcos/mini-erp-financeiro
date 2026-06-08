using System.Net;
using FluentAssertions;

namespace FinFlow.Tests.Integration;

/// <summary>
/// Testes de integracao ponta-a-ponta sobre Oracle real (Testcontainers).
/// Sobe a app, roda o seed e bate nas rotas MVC + /health.
/// Requer Docker. No CI roda no runner ubuntu (Docker disponivel).
/// </summary>
[Collection(OracleCollection.Name)]
public class SmokeTests
{
    private readonly OracleFixture _oracle;
    public SmokeTests(OracleFixture oracle) => _oracle = oracle;

    [Fact]
    public async Task Health_RetornaHealthy()
    {
        await using var factory = new CustomWebAppFactory(_oracle.ConnectionString);
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/health");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resp.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Contas")]
    [InlineData("/Fornecedores")]
    [InlineData("/Relatorios")]
    public async Task Rotas_Principais_Retornam200(string url)
    {
        await using var factory = new CustomWebAppFactory(_oracle.ConnectionString);
        var client = factory.CreateClient();

        var resp = await client.GetAsync(url);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
