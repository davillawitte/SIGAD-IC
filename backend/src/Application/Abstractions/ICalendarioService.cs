using TemplateSistema.Application.Calendario;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Application.Abstractions;

public interface ICalendarioService
{
    Task<IReadOnlyList<CalendarioAnoResumoDto>> ListarAnosAsync(CancellationToken cancellationToken = default);

    Task<Result<CalendarioAnoDto>> ObterAnoAsync(int ano, CancellationToken cancellationToken = default);

    Task<Result<CalendarioAnoDto>> GerarAnoAsync(
        GerarAnoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<CalendarioAnoDto>> AtualizarAnoAsync(
        Guid calendarioAnoId,
        AtualizarAnoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<CalendarioAnoDto>> PublicarAsync(
        Guid calendarioAnoId,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result> ExcluirAnoAsync(
        Guid calendarioAnoId,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<MarcacaoDto>> AdicionarMarcacaoAsync(
        CriarMarcacaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<MarcacaoDto>> AtualizarMarcacaoAsync(
        Guid id,
        AtualizarMarcacaoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result> ExcluirMarcacaoAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiaNaoUtilDto>> ConsultarDiasNaoUteisAsync(
        DateOnly inicio,
        DateOnly fim,
        CancellationToken cancellationToken = default);
}
