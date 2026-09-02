using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Security;
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
        return items.Select(MapLista).ToList();
    }

    public async Task<IReadOnlyList<ServidorListItemDto>> ListMeusAsync(
        string actorLogin,
        bool? semUsuario = null,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (actor.UsuarioId == Guid.Empty)
        {
            return [];
        }

        var query = LoadQuery();
        var setoresVisiveis = actor.SetoresVisiveis(PermissionModules.Servidores);
        if (setoresVisiveis is not null)
        {
            if (setoresVisiveis.Count == 0 && actor.NucleosGerenciadosIds.Count == 0)
            {
                return [];
            }

            // Quem chefia um núcleo enxerga também os servidores lotados nos setores que o
            // núcleo engloba (não só os lotados direto no núcleo) — `SetoresDosNucleosGerenciadosIds`
            // já traz esses setores resolvidos pelo `ActorContextLoader`.
            var setoresIncluidos = setoresVisiveis
                .Concat(actor.SetoresDosNucleosGerenciadosIds)
                .Distinct()
                .ToList();
            var nucleoIdsSet = actor.NucleosGerenciadosIds;

            query = query.Where(x =>
                (x.SetorId != null && setoresIncluidos.Contains(x.SetorId.Value)) ||
                (x.NucleoId != null && nucleoIdsSet.Contains(x.NucleoId.Value)));
        }

        if (semUsuario == true)
        {
            query = query.Where(x => x.Usuario == null && x.Status == StatusServidor.Ativo);
        }

        var items = await query.OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return items.Select(MapLista).ToList();
    }

    public async Task<Result<ServidorDetalheDto>> GetByIdAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidor = await LoadQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result<ServidorDetalheDto>.Failure("Servidor não encontrado.");
        }

        if (!PodeAcessarLotacao(actor, PermissionCodes.ServidoresListar, servidor.SetorId, servidor.NucleoId))
        {
            return Result<ServidorDetalheDto>.Failure("Sem permissão para este servidor.");
        }

        return Result<ServidorDetalheDto>.Success(MapDetalhe(servidor));
    }

    public async Task<Result<ServidorDetalheDto>> CreateAsync(
        CreateServidorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        if (!PodeAcessarLotacao(actor, PermissionCodes.ServidoresCriar, request.SetorId, request.NucleoId))
        {
            return Result<ServidorDetalheDto>.Failure("Sem permissão para cadastrar servidor nesta lotação.");
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
            request.NucleoId,
            null,
            cancellationToken);
        if (validation is not null)
        {
            return Result<ServidorDetalheDto>.Failure(validation);
        }

        var status = request.Status ?? StatusServidor.Ativo;
        var servidor = Servidor.Create(
            request.Nome,
            request.Matricula,
            request.Cpf,
            request.CargoId,
            request.Email,
            request.SetorId,
            request.NucleoId,
            request.DataNascimento,
            request.Telefone,
            status,
            actorLogin);

        db.Servidores.Add(servidor);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(servidor.Id, actorLogin, cancellationToken);
    }

    public async Task<Result<ServidorDetalheDto>> UpdateAsync(
        Guid id,
        UpdateServidorRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidor = await db.Servidores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result<ServidorDetalheDto>.Failure("Servidor não encontrado.");
        }

        if (!PodeAcessarLotacao(actor, PermissionCodes.ServidoresEditar, servidor.SetorId, servidor.NucleoId)
            || !PodeAcessarLotacao(actor, PermissionCodes.ServidoresEditar, request.SetorId, request.NucleoId))
        {
            return Result<ServidorDetalheDto>.Failure("Sem permissão para alterar servidor nesta lotação.");
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
            request.NucleoId,
            id,
            cancellationToken);
        if (validation is not null)
        {
            return Result<ServidorDetalheDto>.Failure(validation);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Result<ServidorDetalheDto>.Failure("Status inválido.");
        }

        servidor.Atualizar(
            request.Nome,
            request.Matricula,
            request.Cpf,
            request.CargoId,
            request.Email,
            request.SetorId,
            request.NucleoId,
            request.DataNascimento,
            request.Telefone,
            actorLogin);
        servidor.DefinirStatus(request.Status, actorLogin);

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, actorLogin, cancellationToken);
    }

    public async Task<Result<ServidorExclusaoImpactoDto>> GetExclusaoImpactoAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidor = await db.Servidores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result<ServidorExclusaoImpactoDto>.Failure("Servidor não encontrado.");
        }

        if (!PodeAcessarLotacao(actor, PermissionCodes.ServidoresExcluir, servidor.SetorId, servidor.NucleoId))
        {
            return Result<ServidorExclusaoImpactoDto>.Failure("Sem permissão para excluir servidor nesta lotação.");
        }

        var impacto = await BuildExclusaoImpactoAsync(id, cancellationToken);
        return Result<ServidorExclusaoImpactoDto>.Success(impacto);
    }

    public async Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default)
    {
        var actor = await ResolveActorAsync(actorLogin, cancellationToken);
        var servidor = await db.Servidores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (servidor is null)
        {
            return Result.Failure("Servidor não encontrado.");
        }

        if (!PodeAcessarLotacao(actor, PermissionCodes.ServidoresExcluir, servidor.SetorId, servidor.NucleoId))
        {
            return Result.Failure("Sem permissão para excluir servidor nesta lotação.");
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

    private Task<ActorContext> ResolveActorAsync(string login, CancellationToken cancellationToken) =>
        ActorContextLoader.LoadAsync(db, login, cancellationToken);

    /// <summary>
    /// Servidor lotado num setor segue a checagem de abrangência por setor de sempre;
    /// servidor lotado direto no núcleo é liberado pra quem chefia aquele núcleo,
    /// ou pra quem tem visão institucional do módulo — nunca cai no branch permissivo
    /// de <see cref="ActorContext.PodeAcessar"/> com setorId nulo.
    /// </summary>
    private static bool PodeAcessarLotacao(ActorContext actor, string code, Guid? setorId, Guid? nucleoId)
    {
        if (nucleoId.HasValue)
        {
            return actor.GerenciaNucleo(nucleoId.Value)
                || (actor.TemVisaoGlobal(PermissionModules.Servidores) && actor.TemPermissao(code));
        }

        return actor.PodeAcessar(code, setorId);
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
        string? email,
        string? telefone,
        DateOnly dataNascimento,
        Guid cargoId,
        Guid? setorId,
        Guid? nucleoId,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (setorId.HasValue == nucleoId.HasValue)
        {
            return "Informe o setor de lotação ou o núcleo de lotação direta, nunca os dois nem nenhum.";
        }

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

        if (setorId.HasValue && !await db.Setores.AnyAsync(x => x.Id == setorId, cancellationToken))
        {
            return "Setor inválido.";
        }

        if (nucleoId.HasValue && !await db.Nucleos.AnyAsync(x => x.Id == nucleoId, cancellationToken))
        {
            return "Núcleo inválido.";
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
            .Include(x => x.Nucleo)
            .Include(x => x.Usuario);

    private static ServidorListItemDto MapLista(Servidor servidor) =>
        new(
            servidor.Id,
            servidor.Nome,
            servidor.Matricula,
            servidor.CargoId,
            servidor.Cargo.Nome,
            servidor.Cargo.Codigo,
            servidor.Email,
            servidor.Telefone,
            servidor.DataNascimento,
            servidor.SetorId,
            servidor.Setor?.Nome,
            servidor.NucleoId,
            servidor.Nucleo?.Nome,
            servidor.Usuario is not null,
            servidor.Usuario?.Ativo ?? false,
            servidor.Status);

    private static ServidorDetalheDto MapDetalhe(Servidor servidor) =>
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
            servidor.Setor?.Nome,
            servidor.NucleoId,
            servidor.Nucleo?.Nome,
            servidor.Usuario is not null,
            servidor.Usuario?.Ativo ?? false,
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
