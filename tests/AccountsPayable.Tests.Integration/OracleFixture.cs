using Testcontainers.Oracle;

namespace AccountsPayable.Tests.Integration;

/// <summary>
/// Sobe um Oracle Free efemero via Testcontainers (exige Docker rodando).
/// Compartilhado por toda a colecao de testes de integracao (sobe 1x).
/// </summary>
public class OracleFixture : IAsyncLifetime
{
    private readonly OracleContainer _container = new OracleBuilder()
        .WithImage("gvenzl/oracle-free:23-slim-faststart")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public class OracleCollection : ICollectionFixture<OracleFixture>
{
    public const string Name = "oracle";
}
