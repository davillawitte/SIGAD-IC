using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Security;

/// <summary>
/// Carrega <see cref="ActorContext"/> a partir do banco. Compartilhado entre
/// <see cref="ActorContextAccessor"/> e serviços que recebem só o DbContext
/// (ex.: testes de integração com <c>new EscalaService(db)</c>).
/// </summary>
public static class ActorContextLoader
{
    public static async Task<ActorContext> LoadAsync(
        ApplicationDbContext db,
        string login,
        CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ActorContext.Empty;
        }

        var usuario = await db.Usuarios
            .AsNoTracking()
            .Include(x => x.UsuarioPerfis)
                .ThenInclude(x => x.Perfil)
                    .ThenInclude(x => x.PerfilPermissoes)
                        .ThenInclude(x => x.Permissao)
            .FirstOrDefaultAsync(x => x.Login == normalized, cancellationToken);

        if (usuario is null)
        {
            return ActorContext.Empty;
        }

        var perfisAtivos = usuario.UsuarioPerfis
            .Where(x => x.Perfil.Ativo)
            .Select(x => x.Perfil)
            .ToList();

        var isSuper = perfisAtivos.Any(x => x.Codigo == PerfilCodes.SuperAdministrador);

        var concessoes = perfisAtivos
            .Select(perfil => new PerfilConcessao(
                perfil.Id,
                perfil.Codigo,
                perfil.PerfilPermissoes
                    .Where(pp => pp.Permissao.Ativo)
                    .Select(pp => (pp.Permissao.Codigo, pp.Abrangencia))))
            .ToList();

        var setoresGerenciados = await db.SetorChefias
            .AsNoTracking()
            .Where(x => x.ServidorId == usuario.ServidorId)
            .Select(x => x.SetorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var nucleosGerenciados = await db.Nucleos
            .AsNoTracking()
            .Where(x => x.ChefeServidorId == usuario.ServidorId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var setoresDosNucleosGerenciados = nucleosGerenciados.Count == 0
            ? []
            : await db.Setores
                .AsNoTracking()
                .Where(x => x.NucleoId != null && nucleosGerenciados.Contains(x.NucleoId.Value))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

        return new ActorContext(
            usuario.Id,
            usuario.ServidorId,
            usuario.Login,
            isSuper,
            setoresGerenciados,
            concessoes,
            nucleosGerenciados,
            setoresDosNucleosGerenciados);
    }
}
