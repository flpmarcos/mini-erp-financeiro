using FluentAssertions;
using Microsoft.Playwright;

namespace FinFlow.Tests.E2E;

/// <summary>
/// Testes E2E (Playwright) que simulam um usuario real no navegador.
/// Requer a app rodando: defina E2E_BASE_URL (ex: http://localhost:5080).
/// Sem essa variavel o teste e ignorado (no-op) para nao quebrar o build local.
///
/// Setup do navegador (uma vez):  pwsh bin/Debug/net8.0/playwright.ps1 install chromium
/// </summary>
public class DashboardE2ETests
{
    private static string? BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL");

    [Fact]
    public async Task Dashboard_CarregaComSidebarEKpis()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) return; // ignorado quando app nao esta no ar

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Dashboard");

        (await page.TitleAsync()).Should().Contain("Dashboard");
        (await page.Locator(".sidebar").CountAsync()).Should().Be(1);
        (await page.Locator(".kpi").CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ListaContas_MostraTabelaEFiltro()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}/Contas");

        (await page.Locator("table").CountAsync()).Should().BeGreaterThan(0);
        (await page.Locator("select[name=Status]").CountAsync()).Should().Be(1);
    }
}
