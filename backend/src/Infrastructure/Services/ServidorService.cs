using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class ServidorService(ApplicationDbContext db) : IServidorService
{
    public async Task<IReadOnlyList<ServidorListItemDto>> ListAsync(
        bool? semUsuario = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Servidores
            .AsNoTracking()
            .Include(x => x.Setor)
            .Include(x => x.Usuario)
            .AsQueryable();

        if (semUsuario == true)
        {
            query = query.Where(x => x.Usuario == null && x.Ativo);
        }

        var items = await query.OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<Result<ServidorListItemDto>> CreateAsync(
        CreateServidorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var cpf = new string(request.Cpf.Where(char.IsDigit).ToArray());
        if (cpf.Length != 11)
        {
            return Result<ServidorListItemDto>.Failure("CPF inválido.");
        }

        if (await db.Servidores.AnyAsync(x => x.Matricula == request.Matricula.Trim(), cancellationToken))
        {
            return Result<ServidorListItemDto>.Failure("Matrícula já cadastrada.");
        }

        if (await db.Servidores.AnyAsync(x => x.Cpf == cpf, cancellationToken))
        {
            return Result<ServidorListItemDto>.Failure("CPF já cadastrado.");
        }

        var setorExists = await db.Setores.AnyAsync(x => x.Id == request.SetorId && x.Ativo, cancellationToken);
        if (!setorExists)
        {
            return Result<ServidorListItemDto>.Failure("Setor inválido.");
        }

        var servidor = Servidor.Create(
            request.Nome,
            request.Matricula,
            cpf,
            request.Cargo,
            request.Email,
            request.SetorId,
            request.Telefone,
            actorLogin);

        db.Servidores.Add(servidor);
        await db.SaveChangesAsync(cancellationToken);

        var created = await db.Servidores
            .Include(x => x.Setor)
            .Include(x => x.Usuario)
            .FirstAsync(x => x.Id == servidor.Id, cancellationToken);

        return Result<ServidorListItemDto>.Success(Map(created));
    }

    private static ServidorListItemDto Map(Servidor servidor) =>
        new(
            servidor.Id,
            servidor.Nome,
            servidor.Matricula,
            servidor.Cpf,
            servidor.Cargo,
            servidor.Email,
            servidor.Telefone,
            servidor.SetorId,
            servidor.Setor.Nome,
            servidor.Usuario is not null,
            servidor.Ativo);
}
