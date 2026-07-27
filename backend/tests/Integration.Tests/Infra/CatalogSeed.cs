using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;
using TemplateSistema.Persistence.Seed;

namespace TemplateSistema.Integration.Tests.Infra;

/// <summary>
/// Semeia o catálogo do sistema para testes: permissões, perfis base (com permissões e
/// abrangências por permissão) e padrões de escala.
/// </summary>
public static class CatalogSeed
{
    public static readonly Guid PerfilSuperAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PerfilChefeSetorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PerfilServidorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PerfilDirecaoIcId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await TipoOcorrenciaSeed.SeedAsync(db, cancellationToken);
        await SeedPermissoesAsync(db, cancellationToken);

        db.Perfis.AddRange(
            Perfil.Create("Super Administrador", PerfilCodes.SuperAdministrador, sistema: true, id: PerfilSuperAdminId),
            Perfil.Create("Chefe de Setor", PerfilCodes.ChefeSetor, sistema: true, id: PerfilChefeSetorId),
            Perfil.Create("Servidor", PerfilCodes.Servidor, sistema: true, id: PerfilServidorId),
            Perfil.Create(
                "Direção do IC",
                PerfilCodes.DirecaoIc,
                "Visão institucional e devolução de escalas",
                sistema: true,
                id: PerfilDirecaoIcId));

        await db.SaveChangesAsync(cancellationToken);

        await SeedChefePermissoesAsync(db, cancellationToken);
        await SeedServidorPermissoesAsync(db, cancellationToken);
        await SeedDirecaoIcPermissoesAsync(db, cancellationToken);
        await SeedSuperAdminPermissoesAsync(db, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await PadraoEscalaSeed.SeedAsync(db, cancellationToken);
    }

    private static async Task SeedPermissoesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        foreach (var (codigo, nome, modulo, area, descricao) in PermissionCodes.Catalog)
        {
            db.Permissoes.Add(Permissao.Create(
                codigo,
                nome,
                modulo,
                area,
                descricao,
                sistema: true,
                createdBy: "catalog-seed",
                id: CreateDeterministicGuid(codigo)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedChefePermissoesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var codes = new[]
        {
            PermissionCodes.EscalasListar,
            PermissionCodes.EscalasCriar,
            PermissionCodes.EscalasEditar,
            PermissionCodes.EscalasFinalizar,
            PermissionCodes.EscalasPublicar,
            PermissionCodes.EscalasExcluir,
            PermissionCodes.EscalasSolicitarDevolucao,
            PermissionCodes.EscalasExportar,
            PermissionCodes.AfastamentosListar,
            PermissionCodes.AfastamentosCriar,
            PermissionCodes.AfastamentosEditar,
            PermissionCodes.AfastamentosExcluir,
            PermissionCodes.ServidoresListar,
        };

        await LinkPermissoesAsync(db, PerfilChefeSetorId, codes, Abrangencia.MeusSetores, cancellationToken);
    }

    private static async Task SeedServidorPermissoesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var codes = new[]
        {
            PermissionCodes.SetoresListar,
            PermissionCodes.CargosListar,
            PermissionCodes.ServidoresListar,
        };

        await LinkPermissoesAsync(db, PerfilServidorId, codes, Abrangencia.MeusSetores, cancellationToken);
    }

    private static async Task SeedDirecaoIcPermissoesAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var leituraGlobal = new[]
        {
            PermissionCodes.EscalasListar,
            PermissionCodes.EscalasDevolver,
            PermissionCodes.EscalasExportar,
            PermissionCodes.AfastamentosListar,
            PermissionCodes.ServidoresListar,
            PermissionCodes.NucleosListar,
            PermissionCodes.SetoresListar,
            PermissionCodes.CargosListar,
        };

        var mutacaoLocal = new[]
        {
            PermissionCodes.EscalasCriar,
            PermissionCodes.EscalasEditar,
            PermissionCodes.EscalasFinalizar,
            PermissionCodes.EscalasPublicar,
            PermissionCodes.EscalasExcluir,
            PermissionCodes.EscalasSolicitarDevolucao,
            PermissionCodes.AfastamentosCriar,
            PermissionCodes.AfastamentosEditar,
            PermissionCodes.AfastamentosExcluir,
        };

        var estruturaGlobal = new[]
        {
            PermissionCodes.NucleosCriar,
            PermissionCodes.NucleosEditar,
            PermissionCodes.NucleosExcluir,
            PermissionCodes.SetoresCriar,
            PermissionCodes.SetoresEditar,
            PermissionCodes.SetoresExcluir,
        };

        await LinkPermissoesAsync(db, PerfilDirecaoIcId, leituraGlobal, Abrangencia.TodosOsSetores, cancellationToken);
        await LinkPermissoesAsync(db, PerfilDirecaoIcId, mutacaoLocal, Abrangencia.MeusSetores, cancellationToken);
        await LinkPermissoesAsync(db, PerfilDirecaoIcId, estruturaGlobal, Abrangencia.TodosOsSetores, cancellationToken);
    }

    private static async Task SeedSuperAdminPermissoesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var ids = await db.Permissoes
            .Where(x => x.Ativo && x.Area == PermissionAreas.AdministracaoDoSistema)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissaoId in ids)
        {
            db.PerfilPermissoes.Add(PerfilPermissao.Create(PerfilSuperAdminId, permissaoId));
        }
    }

    private static async Task LinkPermissoesAsync(
        ApplicationDbContext db,
        Guid perfilId,
        IReadOnlyCollection<string> codes,
        Abrangencia abrangencia,
        CancellationToken cancellationToken)
    {
        var ids = await db.Permissoes
            .Where(x => x.Ativo && codes.Contains(x.Codigo))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissaoId in ids)
        {
            db.PerfilPermissoes.Add(PerfilPermissao.Create(perfilId, permissaoId, abrangencia));
        }
    }

    private static Guid CreateDeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"sigad-ic:{input}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
