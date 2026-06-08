using System.Net;
using System.Net.Http;
using FluentAssertions;

namespace FinFlow.Tests.Integration;

/// <summary>
/// Testes de RBAC (perfis x rotas) sobre a app real com provider InMemory.
/// Nao exige Docker. Usa o esquema de auth de teste (headers X-Test-*).
/// </summary>
public class RbacTests : IClassFixture<InMemoryAuthFactory>
{
    private readonly InMemoryAuthFactory _factory;
    public RbacTests(InMemoryAuthFactory factory) => _factory = factory;

    private HttpClient ClientComo(string? user, string? roles)
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        if (user is not null) client.DefaultRequestHeaders.Add("X-Test-User", user);
        if (roles is not null) client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }

    [Fact]
    public async Task Anonimo_RotaProtegida_NaoAutorizado()
    {
        var resp = await ClientComo(null, null).GetAsync("/Dashboard");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task QualquerAutenticado_VeDashboard()
    {
        var resp = await ClientComo("auditor@demo.com", "Auditor").GetAsync("/Dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Financeiro_PodeAbrirNovaConta()
    {
        var resp = await ClientComo("financeiro@demo.com", "Financeiro").GetAsync("/Contas/Create");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auditor_NaoPodeAbrirNovaConta()
    {
        var resp = await ClientComo("auditor@demo.com", "Auditor").GetAsync("/Contas/Create");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Gerente_AcessaAprovacoes()
    {
        var resp = await ClientComo("gerente@demo.com", "Gerente").GetAsync("/Aprovacoes");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Financeiro_NaoAcessaAprovacoes()
    {
        var resp = await ClientComo("financeiro@demo.com", "Financeiro").GetAsync("/Aprovacoes");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Auditor_AcessaRelatorios()
    {
        var resp = await ClientComo("auditor@demo.com", "Auditor").GetAsync("/Relatorios");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
