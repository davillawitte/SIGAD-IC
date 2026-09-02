using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;

namespace TemplateSistema.Application.Abstractions;

public interface IEscalaResumidaService
{
    Task<PagedResult<EscalaResumidaListItemDto>> ListAsync(EscalaResumidaListQuery query, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> GetByIdAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> CreateAsync(CreateEscalaResumidaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> UpdateAsync(Guid id, UpdateEscalaResumidaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> ConfigurarSetoresAsync(Guid id, ConfigurarSetoresRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> ConfigurarEquipeAsync(Guid id, ConfigurarEquipeRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> AtualizarEquipeAsync(Guid id, Guid equipeId, AtualizarEquipeRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> RemoverEquipeAsync(Guid id, Guid equipeId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> ConfigurarRotacaoAsync(Guid id, Guid equipeId, ConfigurarRotacaoRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> UpsertDiaAsync(Guid id, Guid equipeId, UpsertDiaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> ReverterDiaParaRegraAsync(Guid id, Guid equipeId, DateOnly data, string actorLogin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EscalaResumidaServidorElegivelDto>> ListServidoresElegiveisAsync(Guid? nucleoId, Guid? setorId, string actorLogin, CancellationToken cancellationToken = default);
    Task<EscalaResumidaAnteriorInfoDto?> GetAnteriorAsync(Guid? nucleoId, Guid? setorId, int ano, int mes, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> CopiarAsync(Guid origemId, CopiarEscalaResumidaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> FinalizarAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> ReabrirAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaResumidaDetailDto>> VincularEscalaAsync(Guid id, Guid escalaId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}

public interface IEscalaResumidaPdfService
{
    Task<Result<(byte[] Content, string FileName)>> GenerateAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default);
}
