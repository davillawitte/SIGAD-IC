using Microsoft.EntityFrameworkCore;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Persistence;

namespace TemplateSistema.Integration.Tests.Infra;

[CollectionDefinition(DatabaseCollection.Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

/// <summary>
/// Cada teste recebe um banco próprio, clonado do template. Os contextos são sempre
/// novos: semear, exercitar o serviço e assertar em contextos distintos evita que o
/// change tracker mascare o que de fato foi persistido.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    private TestDatabase _database = null!;

    public async Task InitializeAsync() => _database = await fixture.CreateDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected ApplicationDbContext NewContext() => _database.CreateContext();

    /// <summary>Monta o cenário num contexto dedicado e persiste.</summary>
    protected async Task<T> SemearAsync<T>(Func<CenarioBuilder, T> montar)
    {
        await using var db = NewContext();
        var builder = new CenarioBuilder(db, await CargoDoCatalogoAsync(db));
        var resultado = montar(builder);
        await db.SaveChangesAsync();
        return resultado;
    }

    /// <summary>
    /// Os cargos vêm da migration <c>AddCargoTable</c>, não de um seed de aplicação.
    /// Qual deles é indiferente para estes testes, então pega-se o primeiro em ordem
    /// estável — assim um eventual acerto nos códigos de cargo não quebra a suíte.
    /// </summary>
    protected static Task<Cargo> CargoDoCatalogoAsync(ApplicationDbContext db) =>
        db.Cargos.OrderBy(x => x.Codigo).FirstAsync();

    /// <summary>Os padrões vêm do seed de catálogo com Ids gerados, então busca-se por código.</summary>
    protected async Task<Guid> PadraoIdAsync(string codigo)
    {
        await using var db = NewContext();
        return await db.PadroesEscala
            .Where(x => x.Codigo == codigo)
            .Select(x => x.Id)
            .FirstAsync();
    }

    /// <summary>Ocorrências persistidas de um servidor, ordenadas por data.</summary>
    protected async Task<List<EscalaOcorrencia>> OcorrenciasAsync(Guid servidorId)
    {
        await using var db = NewContext();
        return await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.ServidorId == servidorId)
            .OrderBy(x => x.Data)
            .ToListAsync();
    }
}
