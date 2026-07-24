using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class ServidorService(ApplicationDbContext db) : IServidorService
{
    public async Task<IReadOnlyList<ServidorListItemDto>> ListAsync(
        bool? semUsuario = null,
        CancellationToken cancellationToken = default)
    {
        var query = LoadQuery();

        if (semUsuario == true)
        {
            query = query.Where(x => x.Usuario == null && x.Status == StatusServidor.Ativo);
        }

        var items = await query.OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ServidorListItemDto>> ListMeusAsync(
        string actorLogin,
        bool? semUsuario = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = actorLogin.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios
            .AsNoTracking()
            .Include(x => x.UsuarioPerfis).ThenInclude(x => x.Perfil)
            .FirstOrDefaultAsync(x => x.Login == normalized, cancellationToken);

        if (usuario is null)
        {
            return [];
        }

        var isSuper = usuario.UsuarioPerfis.Any(x =>
            x.Perfil.Ativo && x.Perfil.Codigo == PerfilCodes.SuperAdministrador);

        // SuperAdmin: todos. Demais (inclui Direção IC): somente setores de chefia —
        // criar/editar afastamento não usa visão global.
        if (isSuper)
        {
            return await ListAsync(semUsuario, cancellationToken);
        }

        var setorIds = await db.SetorChefias
            .AsNoTracking()
            .Where(x => x.ServidorId == usuario.ServidorId)
            .Select(x => x.SetorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (setorIds.Count == 0)
        {
            return [];
        }

        var query = LoadQuery().Where(x => setorIds.Contains(x.SetorId));
        if (semUsuario == true)
        {
            query = query.Where(x => x.Usuario == null && x.Status == StatusServidor.Ativo);
        }

        var items = await query.OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<Result<ServidorListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var servidor = await LoadQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return servidor is null
            ? Result<ServidorListItemDto>.Failure("Servidor não encontrado.")
            : Result<ServidorListItemDto>.Success(Map(servidor));
    }

    public async Task<Result<ServidorListItemDto>> CreateAsync(
        CreateServidorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(
            request.Nome,
            request.Cpf,
            request.Matricula,
            request.Email,
            request.Telefone,
            request.DataNascimento,
            request.CargoId,
            request.SetorId,
            null,
            cancellationToken);
        if (validation is not null)
        {
            return Result<ServidorListItemDto>.Failure(validation);
        }

        var status = request.Status ?? StatusServidor.Ativo;
        var servidor = Servidor.Create(
            request.Nome,
            request.Matricula,
            request.Cpf,
            request.CargoId,
            request.Email,
            request.SetorId,
            request.DataNascimento,
            request.Telefone,
            status,
            actorLogin);

        db.Servidores.Add(servidor);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(servidor.Id, cancellationToken);
    }

    public async Task<Result<ServidorListItemDto>> UpdateAsync(
        Guid id,
        UpdateServidorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var servidor = await db.Servidores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result<ServidorListItemDto>.Failure("Servidor não encontrado.");
        }

        var validation = await ValidateAsync(
            request.Nome,
            request.Cpf,
            request.Matricula,
            request.Email,
            request.Telefone,
            request.DataNascimento,
            request.CargoId,
            request.SetorId,
            id,
            cancellationToken);
        if (validation is not null)
        {
            return Result<ServidorListItemDto>.Failure(validation);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Result<ServidorListItemDto>.Failure("Status inválido.");
        }

        servidor.Atualizar(
            request.Nome,
            request.Matricula,
            request.Cpf,
            request.CargoId,
            request.Email,
            request.SetorId,
            request.DataNascimento,
            request.Telefone,
            actorLogin);
        servidor.DefinirStatus(request.Status, actorLogin);

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<ServidorExclusaoImpactoDto>> GetExclusaoImpactoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Servidores.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return Result<ServidorExclusaoImpactoDto>.Failure("Servidor não encontrado.");
        }

        var impacto = await BuildExclusaoImpactoAsync(id, cancellationToken);
        return Result<ServidorExclusaoImpactoDto>.Success(impacto);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var servidor = await db.Servidores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result.Failure("Servidor não encontrado.");
        }

        var impacto = await BuildExclusaoImpactoAsync(id, cancellationToken);
        if (!impacto.PodeExcluir)
        {
            return Result.Failure(
                "Não é possível excluir o servidor enquanto houver vínculos (escalas, afastamentos, chefias ou usuário). Remova os vínculos antes de excluir.");
        }

        // Nucleo.ChefeServidorId usa SetNull no banco — não bloqueia a exclusão.
        db.Servidores.Remove(servidor);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<ServidorExclusaoImpactoDto> BuildExclusaoImpactoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var escalas = await db.EscalaServidores.CountAsync(x => x.ServidorId == id, cancellationToken);
        var afastamentos = await db.Afastamentos.CountAsync(x => x.ServidorId == id, cancellationToken);
        var chefias = await db.SetorChefias.CountAsync(x => x.ServidorId == id, cancellationToken);
        var usuarios = await db.Usuarios.CountAsync(x => x.ServidorId == id, cancellationToken);
        var nucleosComoChefe = await db.Nucleos.CountAsync(x => x.ChefeServidorId == id, cancellationToken);
        var podeExcluir = escalas == 0 && afastamentos == 0 && chefias == 0 && usuarios == 0;

        return new ServidorExclusaoImpactoDto(
            escalas,
            afastamentos,
            chefias,
            usuarios,
            nucleosComoChefe,
            podeExcluir);
    }

    private async Task<string?> ValidateAsync(
        string nome,
        string cpfRaw,
        string matricula,
        string email,
        string? telefone,
        DateOnly dataNascimento,
        Guid cargoId,
        Guid setorId,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Nome é obrigatório.";
        }

        var cpf = Servidor.NormalizeCpf(cpfRaw);
        if (cpf.Length != 11)
        {
            return "CPF inválido.";
        }

        if (!Servidor.IsMatriculaValida(matricula))
        {
            return "Matrícula inválida. Use o formato xxx.xxx-x ou xx.xxx-x.";
        }

        if (!IsEmailValido(email))
        {
            return "E-mail inválido.";
        }

        if (!string.IsNullOrWhiteSpace(telefone))
        {
            var phoneDigits = DigitsOnly(telefone);
            if (phoneDigits.Length is < 10 or > 11)
            {
                return "Telefone inválido.";
            }
        }

        if (dataNascimento == default || dataNascimento > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            return "Data de nascimento inválida.";
        }

        var matriculaTrim = Servidor.NormalizeMatricula(matricula);
        var matriculaExists = excludingId is null
            ? await db.Servidores.AnyAsync(x => x.Matricula == matriculaTrim, cancellationToken)
            : await db.Servidores.AnyAsync(x => x.Id != excludingId && x.Matricula == matriculaTrim, cancellationToken);
        if (matriculaExists)
        {
            return "Matrícula já cadastrada.";
        }

        var cpfExists = excludingId is null
            ? await db.Servidores.AnyAsync(x => x.Cpf == cpf, cancellationToken)
            : await db.Servidores.AnyAsync(x => x.Id != excludingId && x.Cpf == cpf, cancellationToken);
        if (cpfExists)
        {
            return "CPF já cadastrado.";
        }

        if (!await db.Setores.AnyAsync(x => x.Id == setorId, cancellationToken))
        {
            return "Setor inválido.";
        }

        if (!await db.Cargos.AnyAsync(x => x.Id == cargoId && x.Ativo, cancellationToken))
        {
            return "Cargo inválido.";
        }

        return null;
    }

    private IQueryable<Servidor> LoadQuery() =>
        db.Servidores
            .AsNoTracking()
            .Include(x => x.Cargo)
            .Include(x => x.Setor)
            .Include(x => x.Usuario);

    private static ServidorListItemDto Map(Servidor servidor) =>
        new(
            servidor.Id,
            servidor.Nome,
            servidor.Matricula,
            servidor.Cpf,
            servidor.CargoId,
            servidor.Cargo.Nome,
            servidor.Cargo.Codigo,
            servidor.Email,
            servidor.Telefone,
            servidor.DataNascimento,
            servidor.SetorId,
            servidor.Setor.Nome,
            servidor.Usuario is not null,
            servidor.Status);

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());

    private static bool IsEmailValido(string? email)
    {
        // E-mail é opcional; vazio é válido.
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return email.Contains('@') && email.Contains('.');
        }
        catch
        {
            return false;
        }
    }
}
