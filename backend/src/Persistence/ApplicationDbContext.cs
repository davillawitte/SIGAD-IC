using Microsoft.EntityFrameworkCore;

namespace TemplateSistema.Persistence;

/// <summary>
/// DbContext único com schema padrão (public).
/// TODO: Separação por schema/setor (schema-per-sector) é evolução futura planejada —
/// ver README seção "Estratégia de banco de dados".
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
