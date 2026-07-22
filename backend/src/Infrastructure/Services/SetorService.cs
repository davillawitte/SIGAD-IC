using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setores;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class SetorService(ApplicationDbContext db) : ISetorService
{
    public async Task<IReadOnlyList<SetorListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var setores = await LoadQuery().OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return setores.Select(Map).ToList();
    }

    public async Task<Result<SetorListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setor = await LoadQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return setor is null
            ? Result<SetorListItemDto>.Failure("Setor não encontrado.")
            : Result<SetorListItemDto>.Success(Map(setor));
    }

    public async Task<Result<EstruturaOrganizacionalDto>> GetEstruturaAsync(CancellationToken cancellationToken = default)
    {
        var nucleos = await db.Nucleos
            .AsNoTracking()
            .Include(x => x.ChefeServidor)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);

        var setores = await LoadQuery()
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);

        var direcao = setores.FirstOrDefault(x => SetorSiglas.IsDirecaoIc(x.Sigla));
        var nucleosDto = nucleos.Select(n => new NucleoComSetoresDto(
            n.Id,
            n.Nome,
            n.Sigla,
            n.ChefeServidorId,
            n.ChefeServidor?.Nome,
            setores.Where(s => s.NucleoId == n.Id).Select(Map).ToList())).ToList();

        return Result<EstruturaOrganizacionalDto>.Success(
            new EstruturaOrganizacionalDto(nucleosDto, direcao is null ? null : Map(direcao)));
    }

    public async Task<Result<SetorListItemDto>> CreateAsync(
        CreateSetorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var sigla = Setor.NormalizeSigla(request.Sigla);
        var isDirecao = SetorSiglas.IsDirecaoIc(sigla);
        var chefias = request.Chefias ?? [];

        var validation = await ValidateSetorAsync(
            isDirecao,
            request.NucleoId,
            chefias,
            cancellationToken);

        if (validation is not null)
        {
            return Result<SetorListItemDto>.Failure(validation);
        }

        if (isDirecao)
        {
            var siglasExistentes = await db.Setores.Select(x => x.Sigla).ToListAsync(cancellationToken);
            if (siglasExistentes.Any(SetorSiglas.IsDirecaoIc))
            {
                return Result<SetorListItemDto>.Failure("A Direção do IC já está cadastrada.");
            }

            sigla = SetorSiglas.DirecaoIc;
        }
        else if (await SiglaExistsAsync(sigla, excludingId: null, cancellationToken))
        {
            return Result<SetorListItemDto>.Failure("Sigla de setor já existe.");
        }

        var setor = Setor.Create(
            isDirecao ? SetorSiglas.DirecaoIcNome : request.Nome,
            sigla,
            isDirecao ? null : request.NucleoId,
            request.Resumo,
            actorLogin);

        foreach (var chefia in chefias)
        {
            setor.Chefias.Add(SetorChefia.Create(setor.Id, chefia.ServidorId, chefia.TipoChefia));
        }

        db.Setores.Add(setor);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(setor.Id, cancellationToken);
    }

    public async Task<Result<SetorListItemDto>> UpdateAsync(
        Guid id,
        UpdateSetorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var setor = await db.Setores
            .Include(x => x.Chefias)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (setor is null)
        {
            return Result<SetorListItemDto>.Failure("Setor não encontrado.");
        }

        var isDirecao = SetorSiglas.IsDirecaoIc(setor.Sigla);
        var chefias = request.Chefias ?? [];

        var validation = await ValidateSetorAsync(
            isDirecao,
            isDirecao ? null : request.NucleoId,
            chefias,
            cancellationToken);

        if (validation is not null)
        {
            return Result<SetorListItemDto>.Failure(validation);
        }

        if (isDirecao)
        {
            setor.AtualizarResumo(request.Resumo, actorLogin);
        }
        else
        {
            var sigla = Setor.NormalizeSigla(request.Sigla);
            if (SetorSiglas.IsDirecaoIc(sigla))
            {
                return Result<SetorListItemDto>.Failure("A sigla da Direção do IC é reservada.");
            }

            if (await SiglaExistsAsync(sigla, excludingId: id, cancellationToken))
            {
                return Result<SetorListItemDto>.Failure("Sigla de setor já existe.");
            }

            setor.Atualizar(request.Nome, sigla, request.Resumo, request.NucleoId, actorLogin);
        }

        await ReplaceChefiasAsync(setor, chefias, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setor = await db.Setores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (setor is null)
        {
            return Result.Failure("Setor não encontrado.");
        }

        if (SetorSiglas.IsDirecaoIc(setor.Sigla))
        {
            return Result.Failure("A Direção do IC não pode ser excluída.");
        }

        var temServidores = await db.Servidores.AnyAsync(x => x.SetorId == id, cancellationToken);
        if (temServidores)
        {
            return Result.Failure(
                "Operação inválida: não é possível excluir o setor enquanto houver servidores lotados nele.");
        }

        db.Setores.Remove(setor);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task ReplaceChefiasAsync(
        Setor setor,
        IReadOnlyList<SetorChefiaInput> chefias,
        CancellationToken cancellationToken)
    {
        var desired = chefias
            .GroupBy(x => x.TipoChefia)
            .Select(g => g.Last())
            .ToDictionary(x => x.TipoChefia, x => x.ServidorId);

        var existing = setor.Chefias.ToList();
        foreach (var chefia in existing.Where(x => !desired.ContainsKey(x.TipoChefia)))
        {
            db.SetorChefias.Remove(chefia);
        }

        foreach (var (tipo, servidorId) in desired)
        {
            var current = existing.FirstOrDefault(x => x.TipoChefia == tipo);
            if (current is null)
            {
                setor.Chefias.Add(SetorChefia.Create(setor.Id, servidorId, tipo));
            }
            else if (current.ServidorId != servidorId)
            {
                current.TrocarServidor(servidorId);
            }
        }

        await Task.CompletedTask;
        _ = cancellationToken;
    }

    private async Task<string?> ValidateSetorAsync(
        bool isDirecao,
        Guid? nucleoId,
        IReadOnlyList<SetorChefiaInput> chefias,
        CancellationToken cancellationToken)
    {
        if (isDirecao)
        {
            if (nucleoId is not null)
            {
                return "A Direção do IC não pode estar vinculada a um núcleo.";
            }
        }
        else if (nucleoId is null)
        {
            return "Informe o núcleo do setor. Apenas a Direção do IC pode ficar sem núcleo.";
        }
        else
        {
            var nucleoOk = await db.Nucleos.AnyAsync(x => x.Id == nucleoId, cancellationToken);
            if (!nucleoOk)
            {
                return "Núcleo inválido.";
            }
        }

        var allowed = isDirecao
            ? new HashSet<TipoChefia> { TipoChefia.Diretor, TipoChefia.Subcoordenador }
            : new HashSet<TipoChefia> { TipoChefia.ChefiaImediata, TipoChefia.ChefiaSubstituta };

        if (chefias.Any(x => !allowed.Contains(x.TipoChefia)))
        {
            return isDirecao
                ? "A Direção do IC aceita apenas Diretor e Subcoordenador."
                : "Setores comuns aceitam apenas Chefia imediata e Chefia substituta.";
        }

        if (chefias.GroupBy(x => x.TipoChefia).Any(g => g.Count() > 1))
        {
            return "Há mais de um servidor para o mesmo tipo de chefia.";
        }

        if (isDirecao)
        {
            if (chefias.All(x => x.TipoChefia != TipoChefia.Diretor))
            {
                return "Informe o Diretor da Direção do IC.";
            }
        }
        else if (chefias.All(x => x.TipoChefia != TipoChefia.ChefiaImediata))
        {
            return "Informe a chefia imediata do setor.";
        }

        var servidorIds = chefias.Select(x => x.ServidorId).Distinct().ToList();
        if (servidorIds.Count != chefias.Count)
        {
            return "O mesmo servidor não pode ocupar mais de um papel de chefia no setor.";
        }

        var ativos = await db.Servidores
            .Where(x => servidorIds.Contains(x.Id) && x.Status == StatusServidor.Ativo)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (ativos.Count != servidorIds.Count)
        {
            return "Chefias devem referenciar servidores ativos.";
        }

        return null;
    }

    private async Task<bool> SiglaExistsAsync(string sigla, Guid? excludingId, CancellationToken cancellationToken)
    {
        var normalized = SetorSiglas.Normalize(sigla);
        var query = db.Setores.AsQueryable();
        if (excludingId is Guid id)
        {
            query = query.Where(x => x.Id != id);
        }

        var existentes = await query.Select(x => x.Sigla).ToListAsync(cancellationToken);
        return existentes.Any(x => SetorSiglas.Normalize(x) == normalized);
    }

    private IQueryable<Setor> LoadQuery() =>
        db.Setores
            .AsNoTracking()
            .Include(x => x.Nucleo)
            .Include(x => x.Chefias)
            .ThenInclude(x => x.Servidor);

    private static SetorListItemDto Map(Setor setor)
    {
        var isDirecao = SetorSiglas.IsDirecaoIc(setor.Sigla);
        var chefias = setor.Chefias
            .OrderBy(x => x.TipoChefia)
            .Select(x => new SetorChefiaDto(x.TipoChefia, x.ServidorId, x.Servidor?.Nome))
            .ToList();

        return new SetorListItemDto(
            setor.Id,
            setor.Nome,
            setor.Sigla,
            setor.Resumo,
            setor.NucleoId,
            setor.Nucleo?.Nome,
            isDirecao,
            chefias);
    }
}
