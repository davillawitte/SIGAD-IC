using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class AuthService(
    ApplicationDbContext db,
    IPasswordHasherService passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var login = SenhaTemporaria.NormalizeLoginCpf(request.Login);

        var usuario = await db.Usuarios
            .Include(x => x.Servidor)
                .ThenInclude(x => x.Setor)
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

        if (!usuario.Servidor.EstaAtivo)
        {
            return Result<LoginResponse>.Failure("Servidor vinculado inativo.");
        }

        var perfisAtivos = usuario.UsuarioPerfis
            .Where(x => x.Perfil.Ativo)
            .Select(x => x.Perfil)
            .ToList();

        if (perfisAtivos.Count == 0)
        {
            return Result<LoginResponse>.Failure("Usuário sem perfil ativo.");
        }

        var perfis = perfisAtivos
            .Select(x => x.Codigo)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var permissoes = perfisAtivos
            .SelectMany(x => x.PerfilPermissoes)
            .Where(x => x.Permissao.Ativo)
            .Select(x => x.Permissao.Codigo)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var perfisDetalhe = perfisAtivos
            .Select(perfil =>
            {
                var ativos = perfil.PerfilPermissoes
                    .Where(x => x.Permissao.Ativo)
                    .ToList();

                var perfilPermissoes = ativos
                    .Select(x => x.Permissao.Codigo)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                IReadOnlyDictionary<string, Abrangencia> abrangencia = ativos
                    .Where(x => x.Abrangencia != Abrangencia.MeusSetores)
                    .GroupBy(x => x.Permissao.Codigo, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Abrangencia, StringComparer.OrdinalIgnoreCase);

                return new PerfilAuthDto(perfil.Codigo, perfilPermissoes, abrangencia);
            })
            .OrderBy(x => x.Codigo)
            .ToList();

        var setoresGerenciados = await db.SetorChefias
            .AsNoTracking()
            .Where(x => x.ServidorId == usuario.ServidorId)
            .Select(x => x.SetorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var authUser = new UsuarioAuthDto(
            usuario.Id,
            usuario.Login,
            usuario.Servidor.Nome,
            usuario.Servidor.Email,
            perfis,
            permissoes,
            usuario.ServidorId,
            usuario.Servidor.SetorId,
            usuario.Servidor.Setor?.Nome,
            setoresGerenciados,
            usuario.DeveAlterarSenha,
            perfisDetalhe);

        var (token, expires) = jwtTokenService.CreateToken(authUser);
        usuario.RegistrarLogin();
        await db.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(token, expires, authUser));
    }

    public async Task<Result> AlterarSenhaAsync(
        string actorLogin,
        AlterarSenhaRequest request,
        CancellationToken cancellationToken = default)
    {
        var login = SenhaTemporaria.NormalizeLoginCpf(actorLogin);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        if (!passwordHasher.Verify(usuario.SenhaHash, request.SenhaAtual))
        {
            return Result.Failure("Senha atual inválida.");
        }

        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 8)
        {
            return Result.Failure("A nova senha deve ter ao menos 8 caracteres.");
        }

        if (string.Equals(request.SenhaAtual, request.NovaSenha, StringComparison.Ordinal))
        {
            return Result.Failure("A nova senha deve ser diferente da senha atual.");
        }

        usuario.ConfirmarSenhaAlterada(passwordHasher.Hash(request.NovaSenha), actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
