using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Escalas;

public record EscalaListItemDto(
    Guid Id,
    string Identificacao,
    Guid SetorId,
    string SetorNome,
    string SetorSigla,
    int Ano,
    int Mes,
    DateOnly DataInicio,
    DateOnly DataFim,
    TipoFuncionamento TipoFuncionamento,
    StatusEscala Status,
    DateTime? PublicadaEm,
    string? PublicadaPor,
    DateTime CreatedAt,
    string? CreatedBy);

public record EscalaDetailDto(
    Guid Id,
    string Identificacao,
    Guid SetorId,
    string SetorNome,
    string SetorSigla,
    int Ano,
    int Mes,
    DateOnly DataInicio,
    DateOnly DataFim,
    TipoFuncionamento TipoFuncionamento,
    StatusEscala Status,
    string? Observacao,
    DateTime? PublicadaEm,
    string? PublicadaPor,
    DateTime CreatedAt,
    string? CreatedBy,
    decimal CargaHorariaPresencial,
    decimal CargaHorariaRemota,
    IReadOnlyList<EscalaServidorDto> Servidores);

public record EscalaServidorDto(
    Guid Id,
    Guid ServidorId,
    Guid CargoId,
    int Ordem,
    string ServidorNome,
    string Matricula,
    string CargoNome,
    string CargoCodigo,
    decimal CargaHorariaPresencial,
    decimal CargaHorariaRemota,
    IReadOnlyList<EscalaJornadaDto> Jornadas,
    IReadOnlyList<EscalaOcorrenciaDto> Ocorrencias);

public record EscalaJornadaDto(
    Guid Id,
    Guid? PadraoEscalaId,
    string? PadraoEscalaNome,
    TipoJornada TipoJornada,
    DateOnly DataInicio,
    DateOnly DataFim,
    DateOnly? DataInicioCiclo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string TipoOcorrenciaCodigo,
    RecorrenciaTipo RecorrenciaTipo,
    string? DiasSemana,
    int? IntervaloDias,
    int? DiasTrabalho,
    int? DiasFolga,
    string? TipoOcorrenciaFolgaCodigo,
    string? Observacao);

public record EscalaOcorrenciaDto(
    Guid Id,
    DateOnly Data,
    string TipoOcorrenciaCodigo,
    string? TipoOcorrenciaNome,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    OrigemOcorrencia Origem,
    Guid? EscalaJornadaId,
    string? Observacao);

public record EscalaCalendarioDto(
    Guid EscalaId,
    int Ano,
    int Mes,
    DateOnly DataInicio,
    DateOnly DataFim,
    IReadOnlyList<EscalaServidorCalendarioDto> Servidores);

public record EscalaServidorCalendarioDto(
    Guid EscalaServidorId,
    Guid ServidorId,
    string ServidorNome,
    string Matricula,
    string CargoNome,
    string CargoCodigo,
    IReadOnlyList<EscalaOcorrenciaDto> Ocorrencias);

public record TipoOcorrenciaDto(
    string Codigo,
    string Nome,
    decimal? HorasPadrao,
    CategoriaOcorrencia Categoria,
    bool Ativo);

public record PadraoEscalaDto(
    Guid Id,
    string Codigo,
    string Nome,
    TipoFuncionamento TipoFuncionamento,
    TipoJornada TipoJornada,
    RecorrenciaTipo RecorrenciaTipo,
    int? DiasTrabalho,
    int? DiasFolga,
    string? DiasSemana,
    string TipoOcorrenciaTrabalho,
    string TipoOcorrenciaFolga,
    TimeOnly? HoraInicioPadrao,
    TimeOnly? HoraFimPadrao,
    decimal? HorasPadrao,
    bool Sistema,
    bool Ativo);

public record CreateEscalaRequest(
    Guid SetorId,
    int Ano,
    int Mes,
    TipoFuncionamento TipoFuncionamento,
    string? Observacao);

public record UpdateEscalaRequest(
    int Ano,
    int Mes,
    TipoFuncionamento TipoFuncionamento,
    string? Observacao);

public record AddEscalaServidoresRequest(IReadOnlyList<Guid> ServidorIds);

public record CreateEscalaJornadaRequest(
    TipoJornada TipoJornada,
    DateOnly DataInicio,
    DateOnly DataFim,
    string TipoOcorrenciaCodigo,
    RecorrenciaTipo RecorrenciaTipo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string? DiasSemana,
    int? IntervaloDias,
    int? DiasTrabalho,
    int? DiasFolga,
    string? TipoOcorrenciaFolgaCodigo,
    string? Observacao,
    Guid? PadraoEscalaId,
    DateOnly? DataInicioCiclo);

public record GerarEscalaItemRequest(
    Guid ServidorId,
    Guid PadraoEscalaId,
    DateOnly DataInicioCiclo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim);

public record GerarEscalaRequest(
    IReadOnlyList<GerarEscalaItemRequest> Itens,
    bool DistribuirAutomaticamente,
    DateOnly? DataBaseDistribuicao);

public record UpsertOcorrenciaRequest(
    DateOnly Data,
    string TipoOcorrenciaCodigo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string? Observacao);

public record UpsertOcorrenciasLoteRequest(
    IReadOnlyList<Guid> ServidorIds,
    DateOnly DataInicio,
    DateOnly DataFim,
    string TipoOcorrenciaCodigo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string? Observacao,
    bool ConfirmarSobrescrita);

public record SyncOcorrenciaItemRequest(
    Guid ServidorId,
    DateOnly Data,
    string TipoOcorrenciaCodigo,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFim,
    decimal? Horas,
    string? Observacao);

public record SyncOcorrenciasRequest(IReadOnlyList<SyncOcorrenciaItemRequest> Itens);

public record CopiarEscalaRequest(int Ano, int Mes, bool SobrescreverManuais = false);

public record PublicarEscalaRequest(bool ConfirmarConflitos = false);

public record SolicitarDevolucaoEscalaRequest(string Justificativa);

public record ResponderDevolucaoEscalaRequest(string? ObservacaoResposta = null);

public record SolicitacaoDevolucaoEscalaDto(
    Guid Id,
    Guid EscalaId,
    string EscalaIdentificacao,
    Guid SetorId,
    string SetorNome,
    string SetorSigla,
    int Ano,
    int Mes,
    Guid SolicitanteUsuarioId,
    string SolicitanteNome,
    string Justificativa,
    StatusSolicitacaoDevolucao Status,
    string? RespondidoPor,
    DateTime? RespostaEm,
    string? ObservacaoResposta,
    DateTime CreatedAt);

public record EscalaCoberturaDiaDto(
    DateOnly Data,
    IReadOnlyList<string> ServidoresTrabalho,
    bool TemCobertura);

public record EscalaCoberturaDto(
    Guid EscalaId,
    TipoFuncionamento TipoFuncionamento,
    IReadOnlyList<EscalaCoberturaDiaDto> Dias);

public record EscalaConflitoDto(
    string Tipo,
    bool Critico,
    Guid? ServidorId,
    string? ServidorNome,
    DateOnly? Data,
    string Mensagem);

public record EscalaConflitosDto(
    Guid EscalaId,
    IReadOnlyList<EscalaConflitoDto> Itens,
    int TotalCriticos);

public record EscalaAnteriorInfoDto(
    Guid Id,
    int Ano,
    int Mes,
    string Identificacao,
    TipoFuncionamento TipoFuncionamento,
    StatusEscala Status,
    int QuantidadeServidores);

public record EscalaListQuery : PaginationQuery
{
    public Guid? SetorId { get; init; }
    public int? Mes { get; init; }
    public int? Ano { get; init; }
    public StatusEscala? Status { get; init; }
}
