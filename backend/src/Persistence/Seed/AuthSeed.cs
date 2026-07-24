using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Persistence.Seed;

public static class AuthSeed
{
    public static readonly Guid SetorDiretoriaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ServidorVitorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UsuarioVitorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PerfilSuperAdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PerfilChefeSetorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PerfilServidorId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static readonly Guid CargoPeritoCriminalId = CreateDeterministicGuid($"cargo:{CargoCodes.PeritoCriminal}");

    public const string SuperUserLogin = "vitorlopes";
    public const string SuperUserPassword = "Vitor@123";

    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        await SeedPermissoesAsync(context, cancellationToken);
        await SeedPerfisAsync(context, cancellationToken);
        await SeedCargosAsync(context, cancellationToken);
        await SeedSetorServidorUsuarioAsync(context, cancellationToken);
        await TipoOcorrenciaSeed.SeedAsync(context, cancellationToken);
        await PadraoEscalaSeed.SeedAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Auth seed applied (cargos, perfis, permissões, tipos de ocorrência e superusuário).");
    }

    private static async Task SeedPermissoesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Permissoes.ToListAsync(cancellationToken);
        var existingByCodigo = existing.ToDictionary(x => x.Codigo, StringComparer.Ordinal);
        var hasherCreatedBy = "seed";

        foreach (var (codigo, nome, modulo, area, descricao) in PermissionCodes.Catalog)
        {
            if (existingByCodigo.TryGetValue(codigo, out var atual))
            {
                if (!string.Equals(atual.Area, area, StringComparison.Ordinal)
                    || !string.Equals(atual.Nome, nome, StringComparison.Ordinal)
                    || !string.Equals(atual.Modulo, modulo, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(atual.Descricao, descricao, StringComparison.Ordinal))
                {
                    atual.Atualizar(nome, modulo, area, descricao, hasherCreatedBy);
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
                createdBy: hasherCreatedBy,
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

        // Remapear FKs dos códigos longos legados para as siglas e desativar os obsoletos.
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

        var catalogCodes = PermissionCodes.Catalog.Select(x => x.Codigo).ToHashSet(StringComparer.Ordinal);
        var allCatalogPermissaoIds = await context.Permissoes
            .Where(x => x.Ativo && catalogCodes.Contains(x.Codigo))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Super Administrador sempre com o catálogo completo (não editável pela UI).
        await SyncPerfilPermissoesAsync(context, PerfilSuperAdminId, allCatalogPermissaoIds, cancellationToken);

        // Perfis com criar/editar servidores também recebem excluir.
        await EnsureServidoresExcluirForGestoresAsync(context, cancellationToken);

        var chefePermissoes = await context.Permissoes
            .Where(x => x.Ativo && (
                x.Codigo == PermissionCodes.EscalasListar ||
                x.Codigo == PermissionCodes.EscalasCriar ||
                x.Codigo == PermissionCodes.EscalasEditar ||
                x.Codigo == PermissionCodes.EscalasFinalizar ||
                x.Codigo == PermissionCodes.EscalasPublicar ||
                x.Codigo == PermissionCodes.EscalasExcluir ||
                x.Codigo == PermissionCodes.EscalasSolicitarDevolucao ||
                x.Codigo == PermissionCodes.EscalasExportar ||
                x.Codigo == PermissionCodes.AfastamentosListar ||
                x.Codigo == PermissionCodes.AfastamentosCriar ||
                x.Codigo == PermissionCodes.AfastamentosEditar ||
                x.Codigo == PermissionCodes.AfastamentosExcluir))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        // Chefe de Setor: somente Gestão do Setor (sincroniza, remove extras).
        await SyncPerfilPermissoesAsync(context, PerfilChefeSetorId, chefePermissoes, cancellationToken);

        var servidorPermissoes = await context.Permissoes
            .Where(x => x.Ativo && (
                x.Codigo == PermissionCodes.SetoresListar ||
                x.Codigo == PermissionCodes.CargosListar ||
                x.Codigo == PermissionCodes.ServidoresListar))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await EnsurePerfilPermissoesAsync(context, PerfilServidorId, servidorPermissoes, cancellationToken);
    }

    private static async Task EnsureServidoresExcluirForGestoresAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var excluir = await context.Permissoes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ativo && x.Codigo == PermissionCodes.ServidoresExcluir, cancellationToken);
        if (excluir is null)
        {
            return;
        }

        var gestorPermissaoIds = await context.Permissoes
            .AsNoTracking()
            .Where(x =>
                x.Ativo &&
                (x.Codigo == PermissionCodes.ServidoresCriar || x.Codigo == PermissionCodes.ServidoresEditar))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (gestorPermissaoIds.Count == 0)
        {
            return;
        }

        var gestorPerfilIds = await context.PerfilPermissoes
            .AsNoTracking()
            .Where(x =>
                gestorPermissaoIds.Contains(x.PermissaoId) &&
                x.PerfilId != PerfilSuperAdminId)
            .Select(x => x.PerfilId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (gestorPerfilIds.Count == 0)
        {
            return;
        }

        await EnsurePerfilPermissoesAsync(context, gestorPerfilIds, [excluir.Id], cancellationToken);
    }

    private static async Task EnsurePerfilPermissoesAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Guid> perfilIds,
        IReadOnlyCollection<Guid> permissaoIds,
        CancellationToken cancellationToken)
    {
        foreach (var perfilId in perfilIds)
        {
            await EnsurePerfilPermissoesAsync(context, perfilId, permissaoIds, cancellationToken);
        }
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

        var pending = context.ChangeTracker.Entries<PerfilPermissao>()
            .Where(e =>
                e.Entity.PerfilId == perfilId &&
                (e.State == EntityState.Added || e.State == EntityState.Unchanged || e.State == EntityState.Modified))
            .Select(e => e.Entity.PermissaoId);

        var known = existing.Concat(pending).ToHashSet();
        foreach (var permissaoId in permissaoIds.Except(known))
        {
            context.PerfilPermissoes.Add(PerfilPermissao.Create(perfilId, permissaoId));
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

    private static async Task SeedSetorServidorUsuarioAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var setorDirecao = await context.Setores
            .Include(x => x.Chefias)
            .FirstOrDefaultAsync(x => x.Id == SetorDiretoriaId, cancellationToken);

        if (setorDirecao is null)
        {
            context.Setores.Add(Setor.Create(
                SetorSiglas.DirecaoIcNome,
                SetorSiglas.DirecaoIc,
                nucleoId: null,
                resumo: "Direção geral do Instituto de Criminalística",
                createdBy: "seed",
                id: SetorDiretoriaId));
            await context.SaveChangesAsync(cancellationToken);
            setorDirecao = await context.Setores
                .Include(x => x.Chefias)
                .FirstAsync(x => x.Id == SetorDiretoriaId, cancellationToken);
        }
        else
        {
            setorDirecao.Atualizar(
                SetorSiglas.DirecaoIcNome,
                SetorSiglas.DirecaoIc,
                "Direção geral do Instituto de Criminalística",
                nucleoId: null,
                updatedBy: "seed");
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

        // Remove chefias órfãs na Direção IC (ex.: Subcoordenador indevido) e garante só Vitor como Diretor.
        var chefiasNaoDiretor = setorDirecao.Chefias
            .Where(x => x.TipoChefia != TipoChefia.Diretor)
            .ToList();
        if (chefiasNaoDiretor.Count > 0)
        {
            context.SetorChefias.RemoveRange(chefiasNaoDiretor);
        }

        var diretor = setorDirecao.Chefias.FirstOrDefault(x => x.TipoChefia == TipoChefia.Diretor);
        if (diretor is null)
        {
            context.SetorChefias.Add(SetorChefia.Create(SetorDiretoriaId, ServidorVitorId, TipoChefia.Diretor));
        }
        else if (diretor.ServidorId != ServidorVitorId)
        {
            diretor.TrocarServidor(ServidorVitorId);
        }

        await EnforceSingleSetorChefiaPerServidorAsync(context, cancellationToken);

        if (!await context.Usuarios.AnyAsync(x => x.Id == UsuarioVitorId, cancellationToken))
        {
            var hasher = new PasswordHasher<object>();
            var hash = hasher.HashPassword(new object(), SuperUserPassword);

            var usuario = Usuario.Create(
                ServidorVitorId,
                SuperUserLogin,
                hash,
                createdBy: "seed",
                id: UsuarioVitorId,
                deveAlterarSenha: false);

            usuario.DefinirPerfis([PerfilSuperAdminId], "seed");
            context.Usuarios.Add(usuario);
        }
    }

    /// <summary>
    /// Um servidor só pode ser chefe de um setor. Remove vínculos extras (prefere o setor de lotação).
    /// </summary>
    private static async Task EnforceSingleSetorChefiaPerServidorAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var chefias = await context.SetorChefias
            .Include(x => x.Servidor)
            .ToListAsync(cancellationToken);

        var toRemove = new List<SetorChefia>();
        foreach (var group in chefias.GroupBy(x => x.ServidorId))
        {
            var setorIds = group.Select(x => x.SetorId).Distinct().ToList();
            if (setorIds.Count <= 1)
            {
                continue;
            }

            var lotacaoId = group.First().Servidor?.SetorId;
            Guid keepSetorId;
            if (lotacaoId is Guid lotacao && setorIds.Contains(lotacao))
            {
                keepSetorId = lotacao;
            }
            else
            {
                keepSetorId = group
                    .Where(x => x.TipoChefia is TipoChefia.Diretor or TipoChefia.ChefiaImediata)
                    .Select(x => x.SetorId)
                    .FirstOrDefault();
                if (keepSetorId == Guid.Empty)
                {
                    keepSetorId = setorIds[0];
                }
            }

            toRemove.AddRange(group.Where(x => x.SetorId != keepSetorId));
        }

        if (toRemove.Count > 0)
        {
            context.SetorChefias.RemoveRange(toRemove);
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
