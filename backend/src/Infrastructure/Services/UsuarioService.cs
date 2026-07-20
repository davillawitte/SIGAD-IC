using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Usuarios;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class UsuarioService(ApplicationDbContext db, IPasswordHasherService passwordHasher) : IUsuarioService
{
    public async Task<PagedResult<UsuarioListItemDto>> ListAsync(
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var normalized = pagination.Normalize();
        var query = db.Usuarios.AsNoTracking().AsQueryable();

        if (normalized.Search is not null)
        {
            var term = normalized.Search.ToLowerInvariant();
            query = query.Where(x =>
                x.Login.ToLower().Contains(term) ||
                x.Servidor.Nome.ToLower().Contains(term) ||
                x.Servidor.Matricula.ToLower().Contains(term));
        }

        var ordered = query.OrderBy(x => x.Login);
        var totalItems = await ordered.CountAsync(cancellationToken);

        if (totalItems == 0)
        {
            return PagedResult<UsuarioListItemDto>.Empty(normalized.Page, normalized.PageSize);
        }

        var ids = await ordered
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var usuarios = await db.Usuarios
            .AsNoTracking()
            .Include(x => x.Servidor)
            .Include(x => x.UsuarioPerfis)
                .ThenInclude(x => x.Perfil)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var byId = usuarios.ToDictionary(x => x.Id);
        var items = ids
            .Where(byId.ContainsKey)
            .Select(id => MapList(byId[id]))
            .ToList();

        return PagedResult<UsuarioListItemDto>.Create(items, normalized.Page, normalized.PageSize, totalItems);
    }

    public async Task<Result<UsuarioDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await LoadAsync(id, cancellationToken);
        return usuario is null
            ? Result<UsuarioDetailDto>.Failure("Usuário não encontrado.")
            : Result<UsuarioDetailDto>.Success(MapDetail(usuario));
    }

    public async Task<Result<UsuarioDetailDto>> CreateAsync(
        CreateUsuarioRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var login = request.Login.Trim().ToLowerInvariant();

        if (await db.Usuarios.AnyAsync(x => x.Login == login, cancellationToken))
        {
            return Result<UsuarioDetailDto>.Failure("Login já está em uso.");
        }

        var servidor = await db.Servidores.FirstOrDefaultAsync(x => x.Id == request.ServidorId, cancellationToken);
        if (servidor is null || !servidor.Ativo)
        {
            return Result<UsuarioDetailDto>.Failure("Servidor inválido ou inativo.");
        }

        if (await db.Usuarios.AnyAsync(x => x.ServidorId == request.ServidorId, cancellationToken))
        {
            return Result<UsuarioDetailDto>.Failure("Este servidor já possui usuário vinculado.");
        }

        var perfisValidos = await db.Perfis
            .Where(x => request.PerfilIds.Contains(x.Id) && x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (perfisValidos.Count != request.PerfilIds.Distinct().Count())
        {
            return Result<UsuarioDetailDto>.Failure("Um ou mais perfis são inválidos.");
        }

        var usuario = Usuario.Create(
            request.ServidorId,
            login,
            passwordHasher.Hash(request.Senha),
            actorLogin);

        usuario.DefinirPerfis(perfisValidos, actorLogin);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);

        var created = await LoadAsync(usuario.Id, cancellationToken);
        return Result<UsuarioDetailDto>.Success(MapDetail(created!));
    }

    public async Task<Result<UsuarioDetailDto>> UpdateAsync(
        Guid id,
        UpdateUsuarioRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var usuario = await LoadAsync(id, cancellationToken);
        if (usuario is null)
        {
            return Result<UsuarioDetailDto>.Failure("Usuário não encontrado.");
        }

        if (request.PerfilIds is { Count: > 0 })
        {
            var perfisValidos = await db.Perfis
                .Where(x => request.PerfilIds.Contains(x.Id) && x.Ativo)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (perfisValidos.Count != request.PerfilIds.Distinct().Count())
            {
                return Result<UsuarioDetailDto>.Failure("Um ou mais perfis são inválidos.");
            }

            db.UsuarioPerfis.RemoveRange(usuario.UsuarioPerfis);
            foreach (var perfilId in perfisValidos)
            {
                usuario.UsuarioPerfis.Add(UsuarioPerfil.Create(usuario.Id, perfilId));
            }

            usuario.MarkUpdated(actorLogin);
        }

        if (request.Bloqueado == true)
        {
            usuario.Bloquear(actorLogin);
        }
        else if (request.Bloqueado == false)
        {
            usuario.Desbloquear(actorLogin);
        }

        if (request.Ativo == true)
        {
            usuario.Ativar(actorLogin);
        }
        else if (request.Ativo == false)
        {
            usuario.Desativar(actorLogin);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<UsuarioDetailDto>.Success(MapDetail(usuario));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid id,
        ChangePasswordRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 8)
        {
            return Result.Failure("A nova senha deve ter ao menos 8 caracteres.");
        }

        usuario.AlterarSenha(passwordHasher.Hash(request.NovaSenha), actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Usuario?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Usuarios
            .Include(x => x.Servidor)
            .Include(x => x.UsuarioPerfis)
                .ThenInclude(x => x.Perfil)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    private static UsuarioListItemDto MapList(Usuario usuario) =>
        new(
            usuario.Id,
            usuario.Login,
            usuario.Servidor.Nome,
            usuario.Servidor.Matricula,
            usuario.Bloqueado,
            usuario.Ativo,
            usuario.UltimoLogin,
            usuario.UsuarioPerfis.Select(x => x.Perfil.Codigo).OrderBy(x => x).ToList());

    private static UsuarioDetailDto MapDetail(Usuario usuario) =>
        new(
            usuario.Id,
            usuario.ServidorId,
            usuario.Login,
            usuario.Servidor.Nome,
            usuario.Servidor.Matricula,
            usuario.Servidor.Email,
            usuario.Bloqueado,
            usuario.Ativo,
            usuario.UltimoLogin,
            usuario.UsuarioPerfis.Select(x => x.PerfilId).ToList(),
            usuario.UsuarioPerfis.Select(x => x.Perfil.Codigo).OrderBy(x => x).ToList());
}
