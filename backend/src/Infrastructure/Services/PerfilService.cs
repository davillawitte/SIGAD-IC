using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Perfis;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class PerfilService(ApplicationDbContext db) : IPerfilService
{
    private const string PermissaoAdminNaoDelegavel =
        "Permissões de Administração do Sistema não podem ser atribuídas a outros perfis. Elas são exclusivas do Super Administrador.";

    public async Task<PagedResult<PerfilListItemDto>> ListAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var normalized = pagination.Normalize();
        var query = db.Perfis.AsNoTracking().AsQueryable();

        if (normalized.Search is not null)
        {
            var term = normalized.Search.ToLowerInvariant();
            query = query.Where(x =>
                x.Nome.ToLower().Contains(term) ||
                x.Codigo.ToLower().Contains(term) ||
                (x.Descricao != null && x.Descricao.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new PerfilListItemDto(
                x.Id,
                x.Nome,
                x.Codigo,
                x.Descricao,
                x.Sistema,
                x.Ativo,
                x.PerfilPermissoes.Count))
            .ToPagedResultAsync(normalized, cancellationToken);
    }

    public async Task<Result<PerfilDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var perfil = await LoadAsync(id, cancellationToken);
        return perfil is null
            ? Result<PerfilDetailDto>.Failure("Perfil não encontrado.")
            : Result<PerfilDetailDto>.Success(MapDetail(perfil));
    }

    public async Task<Result<PerfilDetailDto>> CreateAsync(
        CreatePerfilRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var codigo = await GenerateUniqueCodigoAsync(request.Nome, request.Codigo, cancellationToken);
        if (await db.Perfis.AnyAsync(x => x.Codigo == codigo, cancellationToken))
        {
            return Result<PerfilDetailDto>.Failure("Código de perfil já existe.");
        }

        var perfil = Perfil.Create(request.Nome, codigo, request.Descricao, sistema: false, createdBy: actorLogin);

        if (request.PermissaoIds is { Count: > 0 })
        {
            var permissoes = await ResolvePermissoesAsync(request.PermissaoIds, cancellationToken);
            if (permissoes is null)
            {
                return Result<PerfilDetailDto>.Failure("Uma ou mais permissões são inválidas.");
            }

            if (ContemAdministracaoDoSistema(permissoes))
            {
                return Result<PerfilDetailDto>.Failure(PermissaoAdminNaoDelegavel);
            }

            foreach (var permissao in permissoes)
            {
                var abr = ResolveAbrangencia(permissao, request.AbrangenciaPorPermissao);
                perfil.PerfilPermissoes.Add(PerfilPermissao.Create(perfil.Id, permissao.Id, abr));
            }

            perfil.MarkUpdated(actorLogin);
        }

        db.Perfis.Add(perfil);
        await db.SaveChangesAsync(cancellationToken);

        var created = await LoadAsync(perfil.Id, cancellationToken);
        return Result<PerfilDetailDto>.Success(MapDetail(created!));
    }

    public async Task<Result<PerfilDetailDto>> UpdateAsync(
        Guid id,
        UpdatePerfilRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var perfil = await LoadAsync(id, cancellationToken);
        if (perfil is null)
        {
            return Result<PerfilDetailDto>.Failure("Perfil não encontrado.");
        }

        perfil.Atualizar(request.Nome, request.Descricao, actorLogin);

        if (request.Ativo == true)
        {
            perfil.Ativar(actorLogin);
        }
        else if (request.Ativo == false)
        {
            try
            {
                perfil.Desativar(actorLogin);
            }
            catch (InvalidOperationException ex)
            {
                return Result<PerfilDetailDto>.Failure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<PerfilDetailDto>.Success(MapDetail(perfil));
    }

    public async Task<Result<PerfilExclusaoImpactoDto>> GetExclusaoImpactoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var perfil = await db.Perfis.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (perfil is null)
        {
            return Result<PerfilExclusaoImpactoDto>.Failure("Perfil não encontrado.");
        }

        if (perfil.Sistema)
        {
            return Result<PerfilExclusaoImpactoDto>.Failure("Perfis de sistema não podem ser desativados.");
        }

        var quantidadeUsuarios = await db.UsuarioPerfis.CountAsync(x => x.PerfilId == id, cancellationToken);
        return Result<PerfilExclusaoImpactoDto>.Success(
            new PerfilExclusaoImpactoDto(quantidadeUsuarios, quantidadeUsuarios > 0));
    }

    public async Task<Result> DesativarAsync(
        Guid id,
        DesativarPerfilRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var perfil = await db.Perfis.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (perfil is null)
        {
            return Result.Failure("Perfil não encontrado.");
        }

        if (perfil.Sistema || perfil.Codigo == PerfilCodes.SuperAdministrador)
        {
            return Result.Failure("Perfis de sistema não podem ser desativados.");
        }

        var vinculos = await db.UsuarioPerfis.Where(x => x.PerfilId == id).ToListAsync(cancellationToken);
        if (vinculos.Count > 0)
        {
            if (request.PerfilSubstitutoId is Guid substitutoId)
            {
                if (substitutoId == id)
                {
                    return Result.Failure("O perfil substituto deve ser diferente do perfil a desativar.");
                }

                var substituto = await db.Perfis.FirstOrDefaultAsync(
                    x => x.Id == substitutoId && x.Ativo,
                    cancellationToken);
                if (substituto is null)
                {
                    return Result.Failure("Perfil substituto não encontrado ou inativo.");
                }

                var usuarioIds = vinculos.Select(x => x.UsuarioId).Distinct().ToList();
                var jaPossuemSubstituto = await db.UsuarioPerfis
                    .Where(x => usuarioIds.Contains(x.UsuarioId) && x.PerfilId == substituto.Id)
                    .Select(x => x.UsuarioId)
                    .ToListAsync(cancellationToken);
                var comSubstituto = jaPossuemSubstituto.ToHashSet();

                db.UsuarioPerfis.RemoveRange(vinculos);

                foreach (var usuarioId in usuarioIds)
                {
                    if (comSubstituto.Add(usuarioId))
                    {
                        db.UsuarioPerfis.Add(UsuarioPerfil.Create(usuarioId, substituto.Id));
                    }
                }
            }
            else if (request.RemoverVinculosSemSubstituto)
            {
                db.UsuarioPerfis.RemoveRange(vinculos);
            }
            else
            {
                return Result.Failure(
                    $"Existem {vinculos.Count} conta(s) vinculada(s) a este perfil. Informe um perfil substituto ou escolha remover os vínculos (usuários ficam sem perfil).");
            }
        }

        try
        {
            perfil.Desativar(actorLogin);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PerfilDetailDto>> SetPermissoesAsync(
        Guid id,
        SetPerfilPermissoesRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var perfil = await LoadAsync(id, cancellationToken);
        if (perfil is null)
        {
            return Result<PerfilDetailDto>.Failure("Perfil não encontrado.");
        }

        if (perfil.Codigo == PerfilCodes.SuperAdministrador)
        {
            return Result<PerfilDetailDto>.Failure(
                "As permissões do Super Administrador não podem ser alteradas. Este perfil possui acesso total.");
        }

        var permissoes = await ResolvePermissoesAsync(request.PermissaoIds, cancellationToken);
        if (permissoes is null)
        {
            return Result<PerfilDetailDto>.Failure("Uma ou mais permissões são inválidas.");
        }

        if (ContemAdministracaoDoSistema(permissoes))
        {
            return Result<PerfilDetailDto>.Failure(PermissaoAdminNaoDelegavel);
        }

        db.PerfilPermissoes.RemoveRange(perfil.PerfilPermissoes);
        foreach (var permissao in permissoes)
        {
            var abr = ResolveAbrangencia(permissao, request.AbrangenciaPorPermissao);
            perfil.PerfilPermissoes.Add(PerfilPermissao.Create(perfil.Id, permissao.Id, abr));
        }

        perfil.MarkUpdated(actorLogin);
        await db.SaveChangesAsync(cancellationToken);

        var updated = await LoadAsync(id, cancellationToken);
        return Result<PerfilDetailDto>.Success(MapDetail(updated!));
    }

    private async Task<string> GenerateUniqueCodigoAsync(
        string nome,
        string? requestedCodigo,
        CancellationToken cancellationToken)
    {
        var baseCodigo = string.IsNullOrWhiteSpace(requestedCodigo)
            ? Perfil.NormalizeCodigo(nome)
            : Perfil.NormalizeCodigo(requestedCodigo);

        if (string.IsNullOrWhiteSpace(baseCodigo))
        {
            baseCodigo = "PERFIL";
        }

        if (baseCodigo.Length > 40)
        {
            baseCodigo = baseCodigo[..40];
        }

        var candidate = baseCodigo;
        var suffix = 0;
        while (await db.Perfis.AnyAsync(x => x.Codigo == candidate, cancellationToken))
        {
            suffix++;
            var suffixText = $"_{suffix}";
            var maxBase = Math.Max(1, 60 - suffixText.Length);
            var truncated = baseCodigo.Length > maxBase ? baseCodigo[..maxBase] : baseCodigo;
            candidate = truncated + suffixText;
        }

        return candidate;
    }

    private async Task<List<Permissao>?> ResolvePermissoesAsync(
        IReadOnlyList<Guid> permissaoIds,
        CancellationToken cancellationToken)
    {
        var distinct = permissaoIds.Distinct().ToList();
        var valid = await db.Permissoes
            .AsNoTracking()
            .Where(x => distinct.Contains(x.Id) && x.Ativo)
            .ToListAsync(cancellationToken);

        return valid.Count == distinct.Count ? valid : null;
    }

    /// <summary>
    /// Permissões de Administração do Sistema não são delegáveis: só o Super Administrador
    /// (perfil de sistema, sincronizado pelo seed) as possui.
    /// </summary>
    private static bool ContemAdministracaoDoSistema(IEnumerable<Permissao> permissoes) =>
        permissoes.Any(x =>
            string.Equals(x.Area, PermissionAreas.AdministracaoDoSistema, StringComparison.Ordinal)
            || PermissionCodes.IsAdministracaoDoSistema(x.Codigo));

    private static Abrangencia ResolveAbrangencia(
        Permissao permissao,
        IReadOnlyDictionary<string, Abrangencia>? porCodigo)
    {
        if (PermissionModules.SemAbrangencia.Contains(permissao.Modulo))
        {
            return Abrangencia.MeusSetores;
        }

        if (porCodigo is not null
            && (porCodigo.TryGetValue(permissao.Codigo, out var byCode)
                || porCodigo.TryGetValue(permissao.Codigo.ToLowerInvariant(), out byCode))
            && Enum.IsDefined(byCode))
        {
            return byCode;
        }

        return Abrangencia.MeusSetores;
    }

    private async Task<Perfil?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Perfis
            .Include(x => x.PerfilPermissoes)
                .ThenInclude(x => x.Permissao)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static PerfilDetailDto MapDetail(Perfil perfil) =>
        new(
            perfil.Id,
            perfil.Nome,
            perfil.Codigo,
            perfil.Descricao,
            perfil.Sistema,
            perfil.Ativo,
            perfil.PerfilPermissoes.Select(x => x.PermissaoId).ToList(),
            perfil.PerfilPermissoes.Select(x => x.Permissao.Codigo).OrderBy(x => x).ToList(),
            perfil.PerfilPermissoes
                .Where(x => x.Abrangencia != Abrangencia.MeusSetores)
                .ToDictionary(x => x.Permissao.Codigo, x => x.Abrangencia, StringComparer.OrdinalIgnoreCase));
}
