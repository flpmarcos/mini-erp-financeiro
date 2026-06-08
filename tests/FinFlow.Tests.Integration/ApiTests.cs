using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FinFlow.Tests.Integration;

/// <summary>Testes da API REST (auth, paginação, envelope) sobre app InMemory.</summary>
public class ApiTests : IClassFixture<InMemoryAuthFactory>
{
    private readonly InMemoryAuthFactory _factory;
    public ApiTests(InMemoryAuthFactory factory) => _factory = factory;

    private HttpClient Auth(string roles = "Financeiro")
    {
        var c = _factory.CreateClient(new() { AllowAutoRedirect = false });
        c.DefaultRequestHeaders.Add("X-Test-User", "tester@demo.com");
        c.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return c;
    }

    [Fact]
    public async Task AccountsPayable_SemAuth_RetornaNaoAutorizado()
    {
        var resp = await _factory.CreateClient(new() { AllowAutoRedirect = false })
            .GetAsync("/api/v1/accounts-payable");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);
    }

    [Fact]
    public async Task AccountsPayable_Autenticado_RetornaPaginado()
    {
        var resp = await Auth().GetAsync("/api/v1/accounts-payable?TamanhoPagina=5");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<ApiEnvelope>();
        body!.success.Should().BeTrue();
        body.meta.Should().NotBeNull();
        body.meta!.pageSize.Should().Be(5);
    }

    [Fact]
    public async Task CashFlow_Autenticado_RetornaOk()
    {
        var resp = await Auth("Auditor").GetAsync("/api/v1/cash-flow");
        var body = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.OK, body[..Math.Min(body.Length, 600)]);
    }

    // Modelos minimos para desserializar o envelope.
    private record ApiEnvelope(bool success, object? data, string? error, MetaModel? meta);
    private record MetaModel(int total, int page, int pageSize, int totalPages);
}
