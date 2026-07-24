using Microsoft.EntityFrameworkCore;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence;

/// <summary>
/// DbContext único com schema padrão (public).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Nucleo> Nucleos => Set<Nucleo>();
    public DbSet<Setor> Setores => Set<Setor>();
    public DbSet<SetorChefia> SetorChefias => Set<SetorChefia>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Servidor> Servidores => Set<Servidor>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<PerfilPermissao> PerfilPermissoes => Set<PerfilPermissao>();
    public DbSet<UsuarioPerfil> UsuarioPerfis => Set<UsuarioPerfil>();
    public DbSet<TipoOcorrencia> TiposOcorrencia => Set<TipoOcorrencia>();
    public DbSet<PadraoEscala> PadroesEscala => Set<PadraoEscala>();
    public DbSet<Escala> Escalas => Set<Escala>();
    public DbSet<EscalaServidor> EscalaServidores => Set<EscalaServidor>();
    public DbSet<EscalaJornada> EscalaJornadas => Set<EscalaJornada>();
    public DbSet<EscalaOcorrencia> EscalaOcorrencias => Set<EscalaOcorrencia>();
    public DbSet<SolicitacaoDevolucaoEscala> SolicitacoesDevolucaoEscala => Set<SolicitacaoDevolucaoEscala>();
    public DbSet<Afastamento> Afastamentos => Set<Afastamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
