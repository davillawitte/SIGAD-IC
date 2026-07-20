using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class AuthService(
    ApplicationDbContext db,
    IPasswordHasherService passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var login = request.Login.Trim().ToLowerInvariant();

        var usuario = await db.Usuarios
            .Include(x => x.Servidor)
            .Include(x => x.UsuarioPerfis)
                .ThenInclude(x => x.Perfil)
                    .ThenInclude(x => x.PerfilPermissoes)
                        .ThenInclude(x => x.Permissao)
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken);

        if (usuario is null || !passwordHasher.Verify(usuario.SenhaHash, request.Senha))
        {
            return Result<LoginResponse>.Failure("Usuário ou senha inválidos.");
        }

        if (!usuario.Ativo || usuario.Bloqueado)
        {
            return Result<LoginResponse>.Failure("Usuário bloqueado ou inativo.");
        }

        if (!usuario.Servidor.Ativo)
        {
            return Result<LoginResponse>.Failure("Servidor vinculado inativo.");
        }

        var perfis = usuario.UsuarioPerfis
            .Where(x => x.Perfil.Ativo)
            .Select(x => x.Perfil.Codigo)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (perfis.Count == 0)
        {
            return Result<LoginResponse>.Failure("Usuário sem perfil ativo.");
        }

        var permissoes = usuario.UsuarioPerfis
            .Where(x => x.Perfil.Ativo)
            .SelectMany(x => x.Perfil.PerfilPermissoes)
            .Where(x => x.Permissao.Ativo)
            .Select(x => x.Permissao.Codigo)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var authUser = new UsuarioAuthDto(
            usuario.Id,
            usuario.Login,
            usuario.Servidor.Nome,
            usuario.Servidor.Email,
            perfis,
            permissoes);

        var (token, expires) = jwtTokenService.CreateToken(authUser);
        usuario.RegistrarLogin();
        await db.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(token, expires, authUser));
    }
}
