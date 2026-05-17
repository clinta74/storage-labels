using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Tests.TestInfrastructure;

public class IntegrationDatabaseFixture : IAsyncLifetime
{
    public IntegrationTestWebAppFactory Factory { get; } = new();

    private Respawner _respawner = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();
        _connectionString = db.Database.GetConnectionString()!;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore =
            [
                new Respawn.Graph.Table("aspnetroles"),
                new Respawn.Graph.Table("aspnetroleclaims"),
                new Respawn.Graph.Table("__EFMigrationsHistory"),
            ]
        });
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
