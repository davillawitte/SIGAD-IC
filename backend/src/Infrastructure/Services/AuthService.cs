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

        var usuario = await LoadUsuarioComPerfisAsync(login, cancellationToken);
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

        var response = await BuildLoginResponseAsync(usuario, registrarLogin: true, cancellationToken);
        return response is null
            ? Result<LoginResponse>.Failure("Usuário sem perfil ativo.")
            : Result<LoginResponse>.Success(response);
    }

    public async Task<Result<LoginResponse>> RefreshSessionAsync(
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var login = SenhaTemporaria.NormalizeLoginCpf(actorLogin);
        var usuario = await LoadUsuarioComPerfisAsync(login, cancellationToken);
        if (usuario is null)
        {
            return Result<LoginResponse>.Failure("Usuário não encontrado.");
        }

        if (!usuario.Ativo || usuario.Bloqueado)
        {
            return Result<LoginResponse>.Failure("Usuário bloqueado ou inativo.");
        }

        if (!usuario.Servidor.EstaAtivo)
        {
            return Result<LoginResponse>.Failure("Servidor vinculado inativo.");
        }

        var response = await BuildLoginResponseAsync(usuario, registrarLogin: false, cancellationToken);
        return response is null
            ? Result<LoginResponse>.Failure("Usuário sem perfil ativo.")
            : Result<LoginResponse>.Success(response);
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

        if (!PasswordPolicy.IsValid(request.NovaSenha, out var erroSenha))
        {
            return Result.Failure(erroSenha!);
        }

        if (string.Equals(request.SenhaAtual, request.NovaSenha, StringComparison.Ordinal))
        {
            return Result.Failure("A nova senha deve ser diferente da senha atual.");
        }

        usuario.ConfirmarSenhaAlterada(passwordHasher.Hash(request.NovaSenha), actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Domain.Entities.Usuario?> LoadUsuarioComPerfisAsync(
        string login,
        CancellationToken cancellationToken) =>
        await db.Usuarios
            .Include(x => x.Servidor)
                .ThenInclude(x => x.Setor)
            .Include(x => x.Servidor)
                .ThenInclude(x => x.Nucleo)
            .Include(x => x.UsuarioPerfis)
                .ThenInclude(x => x.Perfil)
                    .ThenInclude(x => x.PerfilPermissoes)
                        .ThenInclude(x => x.Permissao)
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken);

    private async Task<LoginResponse?> BuildLoginResponseAsync(
        Domain.Entities.Usuario usuario,
        bool registrarLogin,
        CancellationToken cancellationToken)
    {
        var perfisAtivos = usuario.UsuarioPerfis
            .Where(x => x.Perfil.Ativo)
            .Select(x => x.Perfil)
            .ToList();

        if (perfisAtivos.Count == 0)
        {
            return null;
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

        var setoresChefiados = await db.SetorChefias
            .AsNoTracking()
            .Where(x => x.ServidorId == usuario.ServidorId)
            .Select(x => new { x.SetorId, x.Setor.Sigla, x.TipoChefia })
            .Distinct()
            .ToListAsync(cancellationToken);

        var nucleosChefiados = await db.Nucleos
            .AsNoTracking()
            .Where(x => x.ChefeServidorId == usuario.ServidorId)
            .Select(x => new { x.Id, x.Sigla })
            .ToListAsync(cancellationToken);

        var setoresGerenciados = setoresChefiados.Select(x => x.SetorId).ToList();
        var nucleosGerenciados = nucleosChefiados.Select(x => x.Id).ToList();

        var setoresDosNucleosGerenciados = nucleosGerenciados.Count == 0
            ? []
            : await db.Setores
                .AsNoTracking()
                .Where(x => x.NucleoId != null && nucleosGerenciados.Contains(x.NucleoId.Value))
                .Select(x => x.Id)
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
            usuario.Servidor.NucleoId,
            usuario.Servidor.Nucleo?.Nome,
            setoresGerenciados,
            nucleosGerenciados,
            setoresDosNucleosGerenciados,
            usuario.DeveAlterarSenha,
            perfisDetalhe,
            setoresChefiados.Select(x => new ChefiaResumoDto(x.SetorId, x.Sigla, x.TipoChefia)).ToList(),
            nucleosChefiados.Select(x => new ChefiaResumoDto(x.Id, x.Sigla, TipoChefia.ChefiaImediata)).ToList());

        var (token, expires) = jwtTokenService.CreateToken(authUser);
        if (registrarLogin)
        {
            usuario.RegistrarLogin();
            await db.SaveChangesAsync(cancellationToken);
        }

        return new LoginResponse(token, expires, authUser);
    }
}
