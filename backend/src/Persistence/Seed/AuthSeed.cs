using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Persistence.Seed;

/// <summary>
/// Seed de catálogo: permissões, perfis base, cargos, tipos de ocorrência e padrões de escala.
/// Nunca reescreve configuração feita pelo administrador (permissões custom de perfil, chefias,
/// lotações). O único dado de instância que cria é o administrador de bootstrap, e só quando o
/// banco ainda não tem nenhum usuário.
/// </summary>
public static class AuthSeed
{
    public static readonly Guid SetorDiretoriaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ServidorVitorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UsuarioVitorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PerfilSuperAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PerfilChefeSetorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PerfilServidorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid PerfilDirecaoIcId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static readonly Guid CargoPeritoCriminalId = CreateDeterministicGuid($"cargo:{CargoCodes.PeritoCriminal}");

    public const string SuperUserLogin = "vitorlopes";
    public const string SuperUserPassword = "Vitor@123";

    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await SeedPermissoesAsync(context, cancellationToken);
        var perfisCriados = await SeedPerfisAsync(context, cancellationToken);
        await SeedCargosAsync(context, cancellationToken);
        await SeedBootstrapAdminAsync(context, cancellationToken);
        await EnsureSuperAdminComChefiaDirecaoTemPerfilOperacionalAsync(context, cancellationToken);
        await TipoOcorrenciaSeed.SeedAsync(context, cancellationToken);
        await PadraoEscalaSeed.SeedAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        if (perfisCriados.Contains(PerfilDirecaoIcId))
        {
            var atribuidos = await AtribuirPerfilDirecaoIcAsync(context, cancellationToken);
            logger.LogInformation(
                "Perfil Direção IC criado; atribuído a {Quantidade} usuário(s) com chefia na Direção IC.",
                atribuidos);
        }

        logger.LogInformation("Catálogo semeado (permissões, perfis base, cargos, tipos de ocorrência e padrões).");
    }

    private static async Task SeedPermissoesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Permissoes.ToListAsync(cancellationToken);
        var existingByCodigo = existing.ToDictionary(x => x.Codigo, StringComparer.Ordinal);

        foreach (var (codigo, nome, modulo, area, descricao) in PermissionCodes.Catalog)
        {
            if (existingByCodigo.TryGetValue(codigo, out var atual))
            {
                if (!string.Equals(atual.Area, area, StringComparison.Ordinal)
                    || !string.Equals(atual.Nome, nome, StringComparison.Ordinal)
                    || !string.Equals(atual.Modulo, modulo, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(atual.Descricao, descricao, StringComparison.Ordinal))
                {
                    atual.Atualizar(nome, modulo, area, descricao, "seed");
                }

                continue;
            }

            context.Permissoes.Add(Permissao.Create(
                codigo,
                nome,
                modulo,
                area,
                descricao,
                sistema: true,
                createdBy: "seed",
                id: CreateDeterministicGuid(codigo)));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCargosAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var cargos = await context.Cargos.ToListAsync(cancellationToken);
        var byCodigo = cargos.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

        foreach (var (codigo, nome) in CargoCodes.Catalog)
        {
            if (byCodigo.TryGetValue(codigo, out var existing))
            {
                if (!existing.Ativo)
                {
                    existing.Ativar("seed");
                }

                if (!string.Equals(existing.Nome, nome, StringComparison.Ordinal))
                {
                    existing.Atualizar(nome, "seed");
                }

                continue;
            }

            var created = Cargo.Create(
                nome,
                codigo,
                createdBy: "seed",
                id: CreateDeterministicGuid($"cargo:{codigo}"));
            context.Cargos.Add(created);
            cargos.Add(created);
            byCodigo[codigo] = created;
        }

        await context.SaveChangesAsync(cancellationToken);

        cargos = await context.Cargos.ToListAsync(cancellationToken);
        byCodigo = cargos.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

        foreach (var obsolete in cargos.Where(c => CargoCodes.ObsoleteToSigla.ContainsKey(c.Codigo)).ToList())
        {
            if (!CargoCodes.ObsoleteToSigla.TryGetValue(obsolete.Codigo, out var sigla)
                || !byCodigo.TryGetValue(sigla, out var target))
            {
                continue;
            }

            var servidores = await context.Servidores
                .Where(x => x.CargoId == obsolete.Id)
                .ToListAsync(cancellationToken);
            foreach (var servidor in servidores)
            {
                servidor.Atualizar(
                    servidor.Nome,
                    servidor.Matricula,
                    servidor.Cpf,
                    target.Id,
                    servidor.Email,
                    servidor.SetorId,
                    servidor.DataNascimento,
                    servidor.Telefone,
                    "seed");
            }

            var escalaServidores = await context.EscalaServidores
                .Where(x => x.CargoId == obsolete.Id)
                .ToListAsync(cancellationToken);
            foreach (var es in escalaServidores)
            {
                es.AtualizarSnapshot(
                    target.Id,
                    es.ServidorNome,
                    es.Matricula,
                    target.Nome,
                    target.Codigo,
                    "seed");
            }

            if (obsolete.Ativo)
            {
                obsolete.Desativar("seed");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cria perfis base se não existirem. Só sincroniza permissões do SuperAdmin
    /// (Administração do Sistema). Chefe/Servidor/Direção IC: permissões e abrangências só
    /// na criação inicial — depois o administrador manda.
    /// </summary>
    private static async Task<HashSet<Guid>> SeedPerfisAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var criados = new HashSet<Guid>();

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilSuperAdminId, cancellationToken)
            && !await context.Perfis.AnyAsync(x => x.Codigo == PerfilCodes.SuperAdministrador, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Super Administrador",
                PerfilCodes.SuperAdministrador,
                "Administração do sistema (usuários, perfis e permissões)",
                sistema: true,
                createdBy: "seed",
                id: PerfilSuperAdminId));
            criados.Add(PerfilSuperAdminId);
        }

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilChefeSetorId, cancellationToken)
            && !await context.Perfis.AnyAsync(x => x.Codigo == PerfilCodes.ChefeSetor, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Chefe de Setor",
                PerfilCodes.ChefeSetor,
                "Gestão operacional do setor",
                sistema: true,
                createdBy: "seed",
                id: PerfilChefeSetorId));
            criados.Add(PerfilChefeSetorId);
        }

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilServidorId, cancellationToken)
            && !await context.Perfis.AnyAsync(x => x.Codigo == PerfilCodes.Servidor, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Servidor",
                PerfilCodes.Servidor,
                "Acesso básico do servidor",
                sistema: true,
                createdBy: "seed",
                id: PerfilServidorId));
            criados.Add(PerfilServidorId);
        }

        if (!await context.Perfis.AnyAsync(x => x.Id == PerfilDirecaoIcId, cancellationToken)
            && !await context.Perfis.AnyAsync(x => x.Codigo == PerfilCodes.DirecaoIc, cancellationToken))
        {
            context.Perfis.Add(Perfil.Create(
                "Direção IC",
                PerfilCodes.DirecaoIc,
                "Visão institucional e devolução de escalas",
                sistema: true,
                createdBy: "seed",
                id: PerfilDirecaoIcId));
            criados.Add(PerfilDirecaoIcId);
        }

        await context.SaveChangesAsync(cancellationToken);

        var catalogCodes = PermissionCodes.Catalog.Select(x => x.Codigo).ToHashSet(StringComparer.Ordinal);

        // Super Administrador: somente Administração do Sistema (usuários/perfis/permissões).
        // Operação (escalas, afastamentos, etc.) vem de outros perfis do mesmo usuário.
        var adminPermissaoIds = await context.Permissoes
            .Where(x =>
                x.Ativo &&
                catalogCodes.Contains(x.Codigo) &&
                x.Area == PermissionAreas.AdministracaoDoSistema)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var superAdminId = await context.Perfis
            .Where(x => x.Id == PerfilSuperAdminId || x.Codigo == PerfilCodes.SuperAdministrador)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (superAdminId != Guid.Empty)
        {
            await SyncPerfilPermissoesAsync(context, superAdminId, adminPermissaoIds, cancellationToken);
        }

        if (criados.Contains(PerfilChefeSetorId))
        {
            await SeedChefePermissoesIniciaisAsync(context, cancellationToken);
        }

        if (criados.Contains(PerfilServidorId))
        {
            await SeedServidorPermissoesIniciaisAsync(context, cancellationToken);
        }

        if (criados.Contains(PerfilDirecaoIcId))
        {
            await SeedDirecaoIcPermissoesIniciaisAsync(context, cancellationToken);
        }
        else
        {
            // Idempotente: garante baseline (incl. servidores criar/editar/excluir) em bancos já existentes.
            var direcaoId = await context.Perfis
                .Where(x => x.Id == PerfilDirecaoIcId || x.Codigo == PerfilCodes.DirecaoIc)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (direcaoId != Guid.Empty)
            {
                await SeedDirecaoIcPermissoesIniciaisAsync(context, direcaoId, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return criados;
    }

    private static async Task SeedChefePermissoesIniciaisAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var grants = PermissionAreaGrants.Expand([PermissionAreas.GestaoDoSetor]);
        await EnsurePerfilPermissoesFromGrantsAsync(context, PerfilChefeSetorId, grants, cancellationToken);
    }

    private static async Task SeedServidorPermissoesIniciaisAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var codes = new[]
        {
            PermissionCodes.SetoresListar,
            PermissionCodes.CargosListar,
            PermissionCodes.ServidoresListar,
        };

        await EnsurePerfilPermissoesAsync(
            context, PerfilServidorId, codes, Abrangencia.MeusSetores, cancellationToken);
    }

    private static Task SeedDirecaoIcPermissoesIniciaisAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken) =>
        SeedDirecaoIcPermissoesIniciaisAsync(context, PerfilDirecaoIcId, cancellationToken);

    private static async Task SeedDirecaoIcPermissoesIniciaisAsync(
        ApplicationDbContext context,
        Guid perfilId,
        CancellationToken cancellationToken)
    {
        // Institucional (visão de todos) + Setor (CRUD no próprio setor / chefia).
        var grants = PermissionAreaGrants.Expand(
        [
            PermissionAreas.GestaoInstitucional,
            PermissionAreas.GestaoDoSetor,
        ]);
        await EnsurePerfilPermissoesFromGrantsAsync(context, perfilId, grants, cancellationToken);
    }

    private static async Task EnsurePerfilPermissoesFromGrantsAsync(
        ApplicationDbContext context,
        Guid perfilId,
        IReadOnlyDictionary<string, Abrangencia> grants,
        CancellationToken cancellationToken)
    {
        foreach (var group in grants.GroupBy(x => x.Value))
        {
            await EnsurePerfilPermissoesAsync(
                context,
                perfilId,
                group.Select(x => x.Key).ToList(),
                group.Key,
                cancellationToken);
        }
    }

    /// <summary>
    /// Só cria o bootstrap admin se o banco ainda não tiver nenhum usuário.
    /// Não reescreve chefias nem lotação em bancos já em uso.
    /// </summary>
    private static async Task SeedBootstrapAdminAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Usuarios.AnyAsync(cancellationToken))
        {
            return;
        }

        if (!await context.Setores.AnyAsync(x => x.Id == SetorDiretoriaId, cancellationToken))
        {
            context.Setores.Add(Setor.Create(
                SetorSiglas.DirecaoIcNome,
                SetorSiglas.DirecaoIc,
                nucleoId: null,
                resumo: "Direção geral do Instituto de Criminalística",
                createdBy: "seed",
                id: SetorDiretoriaId));
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Servidores.AnyAsync(x => x.Id == ServidorVitorId, cancellationToken))
        {
            var cargoPc = await context.Cargos
                .FirstAsync(x => x.Codigo == CargoCodes.PeritoCriminal, cancellationToken);

            context.Servidores.Add(Servidor.Create(
                nome: "Vitor Lopes",
                matricula: "000.001-0",
                cpf: "00000000000",
                cargoId: cargoPc.Id,
                email: "vitorlopes@pci.rn.gov.br",
                setorId: SetorDiretoriaId,
                dataNascimento: new DateOnly(1990, 1, 1),
                telefone: null,
                createdBy: "seed",
                id: ServidorVitorId));
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.SetorChefias.AnyAsync(
                x => x.SetorId == SetorDiretoriaId && x.ServidorId == ServidorVitorId,
                cancellationToken))
        {
            context.SetorChefias.Add(SetorChefia.Create(SetorDiretoriaId, ServidorVitorId, TipoChefia.Diretor));
        }

        var hasher = new PasswordHasher<object>();
        var hash = hasher.HashPassword(new object(), SuperUserPassword);

        var superAdminId = await context.Perfis
            .Where(x => x.Id == PerfilSuperAdminId || x.Codigo == PerfilCodes.SuperAdministrador)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (superAdminId == Guid.Empty)
        {
            return;
        }

        var usuario = Usuario.Create(
            ServidorVitorId,
            SuperUserLogin,
            hash,
            createdBy: "seed",
            id: UsuarioVitorId,
            deveAlterarSenha: false);

        // SuperAdmin sozinho só administra o sistema; operação vem de outro perfil.
        var perfilIds = new List<Guid> { superAdminId };
        var direcaoIcId = await context.Perfis
            .Where(x => x.Id == PerfilDirecaoIcId || x.Codigo == PerfilCodes.DirecaoIc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (direcaoIcId != Guid.Empty)
        {
            perfilIds.Add(direcaoIcId);
        }

        usuario.DefinirPerfis(perfilIds, "seed");
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// SuperAdmin só carrega Administração do Sistema. Quem também é chefia na Direção IC
    /// precisa do perfil Direção IC para operação (visão/devolver/mutar Meus).
    /// Idempotente: não remove perfis já atribuídos.
    /// </summary>
    private static async Task EnsureSuperAdminComChefiaDirecaoTemPerfilOperacionalAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var superAdminId = await context.Perfis
            .Where(x => x.Id == PerfilSuperAdminId || x.Codigo == PerfilCodes.SuperAdministrador)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var direcaoIcId = await context.Perfis
            .Where(x => x.Id == PerfilDirecaoIcId || x.Codigo == PerfilCodes.DirecaoIc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (superAdminId == Guid.Empty || direcaoIcId == Guid.Empty)
        {
            return;
        }

        var servidorIdsNaDirecao = (await context.SetorChefias
                .AsNoTracking()
                .Select(x => new { x.ServidorId, x.Setor.Sigla })
                .ToListAsync(cancellationToken))
            .Where(x => SetorSiglas.IsDirecaoIc(x.Sigla))
            .Select(x => x.ServidorId)
            .Distinct()
            .ToList();
        if (servidorIdsNaDirecao.Count == 0)
        {
            return;
        }

        var usuarios = await context.Usuarios
            .Include(x => x.UsuarioPerfis)
            .Where(x =>
                servidorIdsNaDirecao.Contains(x.ServidorId) &&
                x.UsuarioPerfis.Any(up => up.PerfilId == superAdminId))
            .ToListAsync(cancellationToken);

        var alterou = false;
        foreach (var usuario in usuarios)
        {
            if (usuario.UsuarioPerfis.Any(x => x.PerfilId == direcaoIcId))
            {
                continue;
            }

            usuario.UsuarioPerfis.Add(UsuarioPerfil.Create(usuario.Id, direcaoIcId));
            alterou = true;
        }

        if (alterou)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// One-shot: ao criar o perfil Direção IC, atribui a quem já é Diretor/Subcoordenador na Direção.
    /// </summary>
    private static async Task<int> AtribuirPerfilDirecaoIcAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var servidorIds = (await context.SetorChefias
                .AsNoTracking()
                .Where(x =>
                    x.TipoChefia == TipoChefia.Diretor || x.TipoChefia == TipoChefia.Subcoordenador)
                .Select(x => new { x.ServidorId, x.Setor.Sigla })
                .ToListAsync(cancellationToken))
            .Where(x => SetorSiglas.IsDirecaoIc(x.Sigla))
            .Select(x => x.ServidorId)
            .Distinct()
            .ToList();

        if (servidorIds.Count == 0)
        {
            return 0;
        }

        var usuarios = await context.Usuarios
            .Include(x => x.UsuarioPerfis)
            .Where(x => servidorIds.Contains(x.ServidorId))
            .ToListAsync(cancellationToken);

        var atribuidos = 0;
        foreach (var usuario in usuarios)
        {
            if (usuario.UsuarioPerfis.Any(x => x.PerfilId == PerfilDirecaoIcId))
            {
                continue;
            }

            usuario.UsuarioPerfis.Add(UsuarioPerfil.Create(usuario.Id, PerfilDirecaoIcId));
            atribuidos++;
        }

        if (atribuidos > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return atribuidos;
    }

    private static async Task EnsurePerfilPermissoesAsync(
        ApplicationDbContext context,
        Guid perfilId,
        IReadOnlyCollection<string> codigos,
        Abrangencia abrangencia,
        CancellationToken cancellationToken)
    {
        var permissoes = await context.Permissoes
            .Where(x => x.Ativo && codigos.Contains(x.Codigo))
            .Select(x => new { x.Id, x.Codigo })
            .ToListAsync(cancellationToken);

        var existing = await context.PerfilPermissoes
            .Where(x => x.PerfilId == perfilId)
            .ToListAsync(cancellationToken);
        var existingByPermissao = existing.ToDictionary(x => x.PermissaoId);

        var pending = context.ChangeTracker.Entries<PerfilPermissao>()
            .Where(e =>
                e.Entity.PerfilId == perfilId &&
                (e.State == EntityState.Added || e.State == EntityState.Unchanged || e.State == EntityState.Modified))
            .Select(e => e.Entity)
            .ToList();

        foreach (var p in pending)
        {
            existingByPermissao.TryAdd(p.PermissaoId, p);
        }

        foreach (var permissao in permissoes)
        {
            if (existingByPermissao.TryGetValue(permissao.Id, out var link))
            {
                link.DefinirAbrangencia(abrangencia);
                continue;
            }

            context.PerfilPermissoes.Add(PerfilPermissao.Create(perfilId, permissao.Id, abrangencia));
        }
    }

    private static async Task SyncPerfilPermissoesAsync(
        ApplicationDbContext context,
        Guid perfilId,
        IReadOnlyCollection<Guid> permissaoIds,
        CancellationToken cancellationToken)
    {
        var desired = permissaoIds.ToHashSet();
        var existing = await context.PerfilPermissoes
            .Where(x => x.PerfilId == perfilId)
            .ToListAsync(cancellationToken);

        var extras = existing.Where(x => !desired.Contains(x.PermissaoId)).ToList();
        if (extras.Count > 0)
        {
            context.PerfilPermissoes.RemoveRange(extras);
        }

        var existingIds = existing.Select(x => x.PermissaoId).ToHashSet();
        foreach (var permissaoId in desired.Except(existingIds))
        {
            context.PerfilPermissoes.Add(PerfilPermissao.Create(perfilId, permissaoId));
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
