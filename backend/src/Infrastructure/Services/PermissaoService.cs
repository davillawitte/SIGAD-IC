using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permissoes;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class PermissaoService(ApplicationDbContext db) : IPermissaoService
{
    public async Task<PagedResult<PermissaoDto>> ListAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var normalized = pagination.Normalize();
        var query = db.Permissoes.AsNoTracking().AsQueryable();

        if (normalized.Search is not null)
        {
            var term = normalized.Search.ToLowerInvariant();
            query = query.Where(x =>
                x.Codigo.ToLower().Contains(term) ||
                x.Nome.ToLower().Contains(term) ||
                x.Modulo.ToLower().Contains(term) ||
                (x.Descricao != null && x.Descricao.ToLower().Contains(term)));
        }

        var paged = await query
            .OrderBy(x => x.Modulo)
            .ThenBy(x => x.Codigo)
            .ToPagedResultAsync(normalized, cancellationToken);

        var items = paged.Items.Select(Map).ToList();
        return PagedResult<PermissaoDto>.Create(items, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<Result<PermissaoDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var permissao = await db.Permissoes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return permissao is null
            ? Result<PermissaoDto>.Failure("Permissão não encontrada.")
            : Result<PermissaoDto>.Success(Map(permissao));
    }

    public async Task<Result<PermissaoDto>> CreateAsync(
        CreatePermissaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var codigo = Permissao.NormalizeCodigo(request.Codigo);
        if (await db.Permissoes.AnyAsync(x => x.Codigo == codigo, cancellationToken))
        {
            return Result<PermissaoDto>.Failure("Código de permissão já existe.");
        }

        var permissao = Permissao.Create(
            codigo,
            request.Nome,
            request.Modulo,
            request.Descricao,
            sistema: false,
            createdBy: actorLogin);

        db.Permissoes.Add(permissao);
        await db.SaveChangesAsync(cancellationToken);
        return Result<PermissaoDto>.Success(Map(permissao));
    }

    public async Task<Result<PermissaoDto>> UpdateAsync(
        Guid id,
        UpdatePermissaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var permissao = await db.Permissoes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (permissao is null)
        {
            return Result<PermissaoDto>.Failure("Permissão não encontrada.");
        }

        permissao.Atualizar(request.Nome, request.Modulo, request.Descricao, actorLogin);

        if (request.Ativo == true)
        {
            permissao.Ativar(actorLogin);
        }
        else if (request.Ativo == false)
        {
            try
            {
                permissao.Desativar(actorLogin);
            }
            catch (InvalidOperationException ex)
            {
                return Result<PermissaoDto>.Failure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<PermissaoDto>.Success(Map(permissao));
    }

    public async Task<Result> SoftDeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var permissao = await db.Permissoes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (permissao is null)
        {
            return Result.Failure("Permissão não encontrada.");
        }

        try
        {
            permissao.Desativar(actorLogin);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static PermissaoDto Map(Permissao permissao) =>
        new(
            permissao.Id,
            permissao.Codigo,
            permissao.Nome,
            permissao.Descricao,
            permissao.Modulo,
            permissao.Sistema,
            permissao.Ativo);
}
