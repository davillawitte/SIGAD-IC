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
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
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
