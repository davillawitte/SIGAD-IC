using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Perfis;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class PerfilService(ApplicationDbContext db) : IPerfilService
{
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
        var codigo = Perfil.NormalizeCodigo(request.Codigo);
        if (await db.Perfis.AnyAsync(x => x.Codigo == codigo, cancellationToken))
        {
            return Result<PerfilDetailDto>.Failure("Código de perfil já existe.");
        }

        var perfil = Perfil.Create(request.Nome, codigo, request.Descricao, sistema: false, createdBy: actorLogin);

        if (request.PermissaoIds is { Count: > 0 })
        {
            var validIds = await ValidatePermissaoIdsAsync(request.PermissaoIds, cancellationToken);
            if (validIds is null)
            {
                return Result<PerfilDetailDto>.Failure("Uma ou mais permissões são inválidas.");
            }

            perfil.DefinirPermissoes(validIds, actorLogin);
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

    public async Task<Result> SoftDeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var perfil = await db.Perfis.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (perfil is null)
        {
            return Result.Failure("Perfil não encontrado.");
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

        var validIds = await ValidatePermissaoIdsAsync(request.PermissaoIds, cancellationToken);
        if (validIds is null)
        {
            return Result<PerfilDetailDto>.Failure("Uma ou mais permissões são inválidas.");
        }

        db.PerfilPermissoes.RemoveRange(perfil.PerfilPermissoes);
        foreach (var permissaoId in validIds)
        {
            perfil.PerfilPermissoes.Add(PerfilPermissao.Create(perfil.Id, permissaoId));
        }

        perfil.MarkUpdated(actorLogin);
        await db.SaveChangesAsync(cancellationToken);

        var updated = await LoadAsync(id, cancellationToken);
        return Result<PerfilDetailDto>.Success(MapDetail(updated!));
    }

    private async Task<List<Guid>?> ValidatePermissaoIdsAsync(
        IReadOnlyList<Guid> permissaoIds,
        CancellationToken cancellationToken)
    {
        var distinct = permissaoIds.Distinct().ToList();
        var valid = await db.Permissoes
            .Where(x => distinct.Contains(x.Id) && x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return valid.Count == distinct.Count ? valid : null;
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
            perfil.PerfilPermissoes.Select(x => x.Permissao.Codigo).OrderBy(x => x).ToList());
}
