using Microsoft.EntityFrameworkCore;
using Npgsql;
using TemplateSistema.Persistence;
using Testcontainers.PostgreSql;

namespace TemplateSistema.Integration.Tests.Infra;

/// <summary>
/// Um container Postgres por execução. As 15 migrations rodam uma única vez num banco
/// template; cada teste clona esse template, o que no Postgres é quase instantâneo.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string TemplateDatabase = "sigad_template";
    private const string MaintenanceDatabase = "postgres";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase(MaintenanceDatabase)
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ExecuteMaintenanceAsync($"""CREATE DATABASE "{TemplateDatabase}";""");

        // Pooling desligado: CREATE DATABASE ... TEMPLATE falha se sobrar qualquer
        // conexão aberta no banco template.
        await using var db = new ApplicationDbContext(BuildOptions(TemplateDatabase, pooling: false));
        await db.Database.MigrateAsync();
        await CatalogSeed.SeedAsync(db);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<TestDatabase> CreateDatabaseAsync()
    {
        var database = $"t_{Guid.NewGuid():N}";
        await ExecuteMaintenanceAsync($"""CREATE DATABASE "{database}" TEMPLATE "{TemplateDatabase}";""");
        return new TestDatabase(this, database);
    }

    public DbContextOptions<ApplicationDbContext> BuildOptions(string database, bool pooling = true) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionStringFor(database, pooling))
            .Options;

    private string ConnectionStringFor(string database, bool pooling) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database,
            Pooling = pooling,
        }.ConnectionString;

    private async Task ExecuteMaintenanceAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionStringFor(MaintenanceDatabase, pooling: false));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

public sealed class TestDatabase(PostgresFixture fixture, string database)
{
    public ApplicationDbContext CreateContext() => new(fixture.BuildOptions(database));
}
