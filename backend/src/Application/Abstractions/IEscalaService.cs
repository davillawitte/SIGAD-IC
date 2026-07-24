using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Abstractions;

public interface IEscalaService
{
    Task<PagedResult<EscalaListItemDto>> ListAsync(EscalaListQuery query, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> GetByIdAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaCalendarioDto>> GetCalendarioAsync(Guid id, Guid? servidorId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> CreateAsync(CreateEscalaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> UpdateAsync(Guid id, UpdateEscalaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> AddServidoresAsync(Guid id, AddEscalaServidoresRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> RemoveServidorAsync(Guid id, Guid servidorId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> AddJornadaAsync(Guid id, Guid servidorId, CreateEscalaJornadaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteJornadaAsync(Guid id, Guid jornadaId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> UpsertOcorrenciaAsync(Guid id, Guid servidorId, UpsertOcorrenciaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteOcorrenciaAsync(Guid id, Guid ocorrenciaId, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> PublicarAsync(Guid id, PublicarEscalaRequest? request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> FinalizarAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> ReabrirAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<SolicitacaoDevolucaoEscalaDto>> SolicitarDevolucaoAsync(Guid id, SolicitarDevolucaoEscalaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> DevolverAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SolicitacaoDevolucaoEscalaDto>> ListDevolucoesPendentesAsync(string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<SolicitacaoDevolucaoEscalaDto>> AprovarDevolucaoAsync(Guid solicitacaoId, ResponderDevolucaoEscalaRequest? request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<SolicitacaoDevolucaoEscalaDto>> RecusarDevolucaoAsync(Guid solicitacaoId, ResponderDevolucaoEscalaRequest? request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> CopiarAsync(Guid id, CopiarEscalaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TipoOcorrenciaDto>> ListTiposOcorrenciaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PadraoEscalaDto>> ListPadroesAsync(TipoFuncionamento? tipo, CancellationToken cancellationToken = default);
    Task<EscalaAnteriorInfoDto?> GetEscalaAnteriorAsync(Guid setorId, int ano, int mes, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> GerarEscalaAsync(Guid id, GerarEscalaRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> AplicarAfastamentosAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaCoberturaDto>> GetCoberturaAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaConflitosDto>> GetConflitosAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> UpsertOcorrenciasLoteAsync(Guid id, UpsertOcorrenciasLoteRequest request, string actorLogin, CancellationToken cancellationToken = default);
    Task<Result<EscalaDetailDto>> SyncOcorrenciasAsync(Guid id, SyncOcorrenciasRequest request, string actorLogin, CancellationToken cancellationToken = default);
}

public interface IEscalaPdfService
{
    Task<Result<(byte[] Content, string FileName)>> GenerateAsync(
        Guid id,
        string layout,
        string actorLogin,
        CancellationToken cancellationToken = default);
}
