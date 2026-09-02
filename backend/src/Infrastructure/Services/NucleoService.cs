using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Nucleos;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class NucleoService(ApplicationDbContext db) : INucleoService
{
    public async Task<IReadOnlyList<NucleoListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Nucleos
            .AsNoTracking()
            .Include(x => x.ChefeServidor)
            .Include(x => x.Setores)
            .OrderBy(x => x.Nome)
            .Select(x => new NucleoListItemDto(
                x.Id,
                x.Nome,
                x.Sigla,
                x.ChefeServidorId,
                x.ChefeServidor != null ? x.ChefeServidor.Nome : null,
                x.Setores.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NucleoListItemDto>> ListMeusAsync(
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var normalized = actorLogin.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Login == normalized, cancellationToken);

        if (usuario is null)
        {
            return [];
        }

        return await db.Nucleos
            .AsNoTracking()
            .Where(x => x.ChefeServidorId == usuario.ServidorId)
            .Include(x => x.ChefeServidor)
            .Include(x => x.Setores)
            .OrderBy(x => x.Nome)
            .Select(x => new NucleoListItemDto(
                x.Id,
                x.Nome,
                x.Sigla,
                x.ChefeServidorId,
                x.ChefeServidor != null ? x.ChefeServidor.Nome : null,
                x.Setores.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<NucleoDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nucleo = await db.Nucleos
            .AsNoTracking()
            .Include(x => x.ChefeServidor)
            .Include(x => x.Setores)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return nucleo is null
            ? Result<NucleoDetailDto>.Failure("Núcleo não encontrado.")
            : Result<NucleoDetailDto>.Success(MapDetail(nucleo));
    }

    public async Task<Result<NucleoDetailDto>> CreateAsync(
        CreateNucleoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Result<NucleoDetailDto>.Failure("Nome do núcleo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Result<NucleoDetailDto>.Failure("Sigla do núcleo é obrigatória.");
        }

        var sigla = Nucleo.NormalizeSigla(request.Sigla);
        if (await SiglaExistsAsync(sigla, excludingId: null, cancellationToken))
        {
            return Result<NucleoDetailDto>.Failure("Sigla de núcleo já existe.");
        }

        if (request.ChefeServidorId is Guid chefeId)
        {
            var chefeOk = await db.Servidores.AnyAsync(x => x.Id == chefeId && x.Status == StatusServidor.Ativo, cancellationToken);
            if (!chefeOk)
            {
                return Result<NucleoDetailDto>.Failure("Chefe inválido.");
            }
        }

        var nucleo = Nucleo.Create(request.Nome, sigla, request.ChefeServidorId, actorLogin);
        db.Nucleos.Add(nucleo);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(nucleo.Id, cancellationToken);
    }

    public async Task<Result<NucleoDetailDto>> UpdateAsync(
        Guid id,
        UpdateNucleoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var nucleo = await db.Nucleos
            .Include(x => x.ChefeServidor)
            .Include(x => x.Setores)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (nucleo is null)
        {
            return Result<NucleoDetailDto>.Failure("Núcleo não encontrado.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Result<NucleoDetailDto>.Failure("Nome do núcleo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Result<NucleoDetailDto>.Failure("Sigla do núcleo é obrigatória.");
        }

        var sigla = Nucleo.NormalizeSigla(request.Sigla);
        if (await SiglaExistsAsync(sigla, excludingId: id, cancellationToken))
        {
            return Result<NucleoDetailDto>.Failure("Sigla de núcleo já existe.");
        }

        if (request.ChefeServidorId is Guid chefeId)
        {
            var chefeOk = await db.Servidores.AnyAsync(x => x.Id == chefeId && x.Status == StatusServidor.Ativo, cancellationToken);
            if (!chefeOk)
            {
                return Result<NucleoDetailDto>.Failure("Chefe inválido.");
            }
        }

        nucleo.Atualizar(request.Nome, sigla, request.ChefeServidorId, actorLogin);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var nucleo = await db.Nucleos
            .Include(x => x.Setores)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (nucleo is null)
        {
            return Result.Failure("Núcleo não encontrado.");
        }

        if (nucleo.Setores.Count > 0)
        {
            return Result.Failure("Não é possível excluir o núcleo enquanto houver setores vinculados. Remova os setores antes.");
        }

        db.Nucleos.Remove(nucleo);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<bool> SiglaExistsAsync(string sigla, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalized = SetorSiglas.Normalize(sigla);
        var query = db.Nucleos.AsQueryable();
        if (excludingId is Guid id)
        {
            query = query.Where(x => x.Id != id);
        }

        var existentes = await query.Select(x => x.Sigla).ToListAsync(cancellationToken);
        return existentes.Any(x => SetorSiglas.Normalize(x) == normalized);
    }

    private static NucleoDetailDto MapDetail(Nucleo nucleo) =>
        new(
            nucleo.Id,
            nucleo.Nome,
            nucleo.Sigla,
            nucleo.ChefeServidorId,
            nucleo.ChefeServidor?.Nome,
            nucleo.Setores.Select(x => x.Id).ToList());
}
