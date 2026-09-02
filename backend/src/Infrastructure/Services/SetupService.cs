using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setup;
using TemplateSistema.Domain.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;
using TemplateSistema.Persistence.Seed;

namespace TemplateSistema.Infrastructure.Services;

public class SetupService(
    ApplicationDbContext db,
    IPasswordHasherService passwordHasher,
    IAuthService authService) : ISetupService
{
    public async Task<SetupStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        new(!await ExisteSuperAdminAtivoAsync(cancellationToken));

    public async Task<Result<LoginResponse>> CompleteAsync(
        CompleteSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await ExisteSuperAdminAtivoAsync(cancellationToken))
        {
            return Result<LoginResponse>.Failure("Setup já foi concluído.");
        }

        var tokenHash = SetupTokenGenerator.HashToken(request.Token);
        var token = await db.SetupTokens
            .Where(x => x.TokenHash == tokenHash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (token is null || !token.EstaValido(DateTime.UtcNow))
        {
            return Result<LoginResponse>.Failure("Token inválido ou expirado.");
        }

        var cpf = Servidor.NormalizeCpf(request.Cpf);
        if (await db.Servidores.AnyAsync(x => x.Cpf == cpf, cancellationToken))
        {
            return Result<LoginResponse>.Failure("CPF já cadastrado.");
        }

        var cargoId = await db.Cargos
            .Where(x => x.Codigo == CargoCodes.PeritoCriminal)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (cargoId == Guid.Empty)
        {
            return Result<LoginResponse>.Failure("Catálogo de cargos não inicializado.");
        }

        var setorId = await db.Setores
            .Where(x => x.Id == AuthSeed.SetorDiretoriaId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (setorId == Guid.Empty)
        {
            return Result<LoginResponse>.Failure("Setor Direção IC não inicializado.");
        }

        var superAdminPerfilId = await db.Perfis
            .Where(x => x.Codigo == PerfilCodes.SuperAdministrador)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (superAdminPerfilId == Guid.Empty)
        {
            return Result<LoginResponse>.Failure("Catálogo de perfis não inicializado.");
        }

        Servidor servidor;
        try
        {
            servidor = Servidor.Create(
                request.Nome,
                request.Matricula,
                cpf,
                cargoId,
                request.Email,
                setorId,
                nucleoId: null,
                request.DataNascimento,
                telefone: null,
                createdBy: "setup");
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Failure(ex.Message);
        }

        db.Servidores.Add(servidor);

        var usuario = Usuario.Create(
            servidor.Id,
            cpf,
            passwordHasher.Hash(request.Senha),
            createdBy: "setup",
            deveAlterarSenha: true);

        var perfilIds = new List<Guid> { superAdminPerfilId };
        var direcaoIcPerfilId = await db.Perfis
            .Where(x => x.Codigo == PerfilCodes.DirecaoIc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (direcaoIcPerfilId != Guid.Empty)
        {
            perfilIds.Add(direcaoIcPerfilId);
        }

        usuario.DefinirPerfis(perfilIds, "setup");
        db.Usuarios.Add(usuario);

        db.SetorChefias.Add(SetorChefia.Create(setorId, servidor.Id, TipoChefia.Diretor));

        token.MarcarUsado();

        await db.SaveChangesAsync(cancellationToken);

        return await authService.LoginAsync(new LoginRequest(cpf, request.Senha), cancellationToken);
    }

    private async Task<bool> ExisteSuperAdminAtivoAsync(CancellationToken cancellationToken) =>
        await db.Usuarios
            .Where(u => u.Ativo && !u.Bloqueado)
            .AnyAsync(
                u => u.UsuarioPerfis.Any(up => up.Perfil.Ativo && up.Perfil.Codigo == PerfilCodes.SuperAdministrador),
                cancellationToken);
}
