using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Persistence.Seed;

public static class AuthSeed
{
    public static readonly Guid SetorDiretoriaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ServidorVitorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UsuarioVitorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PerfilSuperAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PerfilChefeSetorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PerfilServidorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public const string SuperUserLogin = "vitorlopes";
    public const string SuperUserPassword = "Vitor@123";

    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await SeedPermissoesAsync(context, cancellationToken);
        await SeedPerfisAsync(context, cancellationToken);
        await SeedSetorServidorUsuarioAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Auth seed applied (perfis, permissões e superusuário).");
    }

    private static async Task SeedPermissoesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Permissoes.Select(x => x.Codigo).ToListAsync(cancellationToken);
        var hasherCreatedBy = "seed";

        foreach (var (codigo, nome, modulo, descricao) in PermissionCodes.Catalog)
        {
            if (existing.Contains(codigo))
            {
                continue;
            }

            context.Permissoes.Add(Permissao.Create(
                codigo,
                nome,
                modulo,
                descricao,
                sistema: true,
                createdBy: hasherCreatedBy,
                id: CreateDeterministicGuid(codigo)));
        }
    }

    private static async Task SeedPerfisAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilSuperAdminId, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Super Administrador",
                PerfilCodes.SuperAdministrador,
                "Acesso total à plataforma",
                sistema: true,
                createdBy: "seed",
                id: PerfilSuperAdminId));
        }

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilChefeSetorId, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Chefe de Setor",
                PerfilCodes.ChefeSetor,
                "Gestão operacional do setor",
                sistema: true,
                createdBy: "seed",
                id: PerfilChefeSetorId));
        }

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilServidorId, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Servidor",
                PerfilCodes.Servidor,
                "Acesso básico do servidor",
                sistema: true,
                createdBy: "seed",
                id: PerfilServidorId));
        }

        await context.SaveChangesAsync(cancellationToken);

        var allPermissaoIds = await context.Permissoes
            .Where(x => x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await EnsurePerfilPermissoesAsync(context, PerfilSuperAdminId, allPermissaoIds, cancellationToken);

        var chefePermissoes = await context.Permissoes
            .Where(x => x.Ativo && (
                x.Codigo == PermissionCodes.UsuariosListar ||
                x.Codigo == PermissionCodes.SetoresListar ||
                x.Codigo == PermissionCodes.ServidoresListar ||
                x.Codigo == PermissionCodes.ServidoresCriar ||
                x.Codigo == PermissionCodes.ServidoresEditar ||
                x.Codigo == PermissionCodes.PerfisListar ||
                x.Codigo == PermissionCodes.PermissoesListar))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await EnsurePerfilPermissoesAsync(context, PerfilChefeSetorId, chefePermissoes, cancellationToken);

        var servidorPermissoes = await context.Permissoes
            .Where(x => x.Ativo && (
                x.Codigo == PermissionCodes.SetoresListar ||
                x.Codigo == PermissionCodes.ServidoresListar))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await EnsurePerfilPermissoesAsync(context, PerfilServidorId, servidorPermissoes, cancellationToken);
    }

    private static async Task EnsurePerfilPermissoesAsync(
        ApplicationDbContext context,
        Guid perfilId,
        IReadOnlyCollection<Guid> permissaoIds,
        CancellationToken cancellationToken)
    {
        var existing = await context.PerfilPermissoes
            .Where(x => x.PerfilId == perfilId)
            .Select(x => x.PermissaoId)
            .ToListAsync(cancellationToken);

        foreach (var permissaoId in permissaoIds.Except(existing))
        {
            context.PerfilPermissoes.Add(PerfilPermissao.Create(perfilId, permissaoId));
        }
    }

    private static async Task SeedSetorServidorUsuarioAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Setores.AnyAsync(x => x.Id == SetorDiretoriaId, cancellationToken))
        {
            context.Setores.Add(Setor.Create(
                "Diretoria",
                "DIR",
                createdBy: "seed",
                id: SetorDiretoriaId));
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Servidores.AnyAsync(x => x.Id == ServidorVitorId, cancellationToken))
        {
            context.Servidores.Add(Servidor.Create(
                nome: "Vitor Lopes",
                matricula: "000001",
                cpf: "00000000000",
                cargo: "Super Administrador",
                email: "vitorlopes@pci.rn.gov.br",
                setorId: SetorDiretoriaId,
                telefone: null,
                createdBy: "seed",
                id: ServidorVitorId));
            await context.SaveChangesAsync(cancellationToken);

            var setor = await context.Setores.FirstAsync(x => x.Id == SetorDiretoriaId, cancellationToken);
            setor.DefinirChefe(ServidorVitorId, "seed");
        }

        if (!await context.Usuarios.AnyAsync(x => x.Id == UsuarioVitorId, cancellationToken))
        {
            var hasher = new PasswordHasher<object>();
            var hash = hasher.HashPassword(new object(), SuperUserPassword);

            var usuario = Usuario.Create(
                ServidorVitorId,
                SuperUserLogin,
                hash,
                createdBy: "seed",
                id: UsuarioVitorId);

            usuario.DefinirPerfis([PerfilSuperAdminId], "seed");
            context.Usuarios.Add(usuario);
        }
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"sigad-ic:{input}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
