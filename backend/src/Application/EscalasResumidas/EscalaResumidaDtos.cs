using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.EscalasResumidas;

public record EscalaResumidaListItemDto(
    Guid Id,
    string Identificacao,
    Guid? NucleoId,
    string? NucleoNome,
    string? NucleoSigla,
    int Ano,
    int Mes,
    DateOnly DataInicio,
    DateOnly DataFim,
    StatusEscala Status,
    DateTime CreatedAt,
    string? CreatedBy,
    int QuantidadeSetores,
    IReadOnlyList<string> SetoresSiglas,
    Guid? SetorId = null,
    string? SetorNome = null,
    string? SetorSigla = null);

public record EscalaResumidaDetailDto(
    Guid Id,
    string Identificacao,
    Guid? NucleoId,
    string? NucleoNome,
    string? NucleoSigla,
    int Ano,
    int Mes,
    DateOnly DataInicio,
    DateOnly DataFim,
    StatusEscala Status,
    string? Observacao,
    Guid? EscalaId,
    DateTime CreatedAt,
    string? CreatedBy,
    IReadOnlyList<EscalaResumidaSetorDto> Setores,
    Guid? SetorId = null,
    string? SetorNome = null,
    string? SetorSigla = null);

/// <summary>`SetorId` nulo representa o grupo "Agentes" (servidores lotados direto no
/// núcleo, sem setor específico).</summary>
public record EscalaResumidaSetorDto(
    Guid Id,
    Guid? SetorId,
    string SetorNome,
    string SetorSigla,
    int Ordem,
    IReadOnlyList<EscalaResumidaEquipeDto> Equipes);

public record EscalaResumidaEquipeDto(
    Guid Id,
    string Nome,
    int Ordem,
    DateOnly? DataInicioCiclo,
    IReadOnlyList<EscalaResumidaRotacaoMembroDto> Rotacao,
    IReadOnlyList<EscalaResumidaDiaDto> Dias);

/// <summary>`ServidorId2` é um reforço opcional na mesma posição (ex.: vaga de Agentes com
/// duas pessoas) — hoje só faz sentido pro grupo Agentes, mas não é validado por setor aqui.</summary>
public record EscalaResumidaRotacaoMembroDto(
    Guid Id,
    int Posicao,
    Guid? ServidorId,
    string? ServidorNome,
    Guid? ServidorId2,
    string? ServidorNome2);

public record EscalaResumidaDiaDto(
    Guid Id,
    DateOnly Data,
    Guid? ServidorId,
    string? ServidorNome,
    Guid? ServidorId2,
    string? ServidorNome2,
    bool IsFolga2,
    string? TextoLivre,
    bool IsFolga,
    string Rotulo,
    OrigemOcorrencia Origem,
    Guid? RotacaoMembroId);

/// <summary>Pool de servidores elegíveis pra rodízio do núcleo: chefe do núcleo, servidores
/// lotados direto no núcleo, e servidores de qualquer setor que o núcleo engloba.</summary>
public record EscalaResumidaServidorElegivelDto(
    Guid Id,
    string Nome,
    string Matricula,
    Guid? SetorId,
    string? SetorNome);

/// <summary>Exatamente um entre <paramref name="NucleoId"/> e <paramref name="SetorId"/>
/// deve vir preenchido — ver <see cref="TemplateSistema.Domain.Entities.EscalaResumida"/>.</summary>
public record CreateEscalaResumidaRequest(
    Guid? NucleoId,
    int Ano,
    int Mes,
    string? Observacao,
    Guid? SetorId = null);

public record UpdateEscalaResumidaRequest(string? Observacao);

/// <summary>`SetorId` nulo pede o grupo "Agentes" (só um por escala resumida).</summary>
public record ConfigurarSetorItem(Guid? SetorId, int Ordem);

public record ConfigurarSetoresRequest(IReadOnlyList<ConfigurarSetorItem> Setores);

/// <summary>Nome/ordem NÃO vêm do cliente — são sempre derivados no servidor a partir de
/// quantas equipes o setor já tem ("Equipe 01", "Equipe 02", ...), pra numeração nunca
/// depender de uma contagem que o front calculou (por setor errado, corrida de cliques etc.).</summary>
public record ConfigurarEquipeRequest(Guid EscalaResumidaSetorId);

public record AtualizarEquipeRequest(string Nome, int Ordem);

public record RotacaoMembroItem(int Posicao, Guid? ServidorId, Guid? ServidorId2 = null);

public record ConfigurarRotacaoRequest(DateOnly DataInicioCiclo, IReadOnlyList<RotacaoMembroItem> Membros);

public record UpsertDiaRequest(
    DateOnly Data,
    Guid? ServidorId,
    string? TextoLivre,
    bool IsFolga,
    Guid? ServidorId2 = null,
    bool IsFolga2 = false);

public record CopiarEscalaResumidaRequest(int Ano, int Mes);

public record VincularEscalaRequest(Guid EscalaId);

public record EscalaResumidaAnteriorInfoDto(
    Guid Id,
    int Ano,
    int Mes,
    string Identificacao,
    StatusEscala Status,
    int QuantidadeSetores);

public record EscalaResumidaListQuery : PaginationQuery
{
    public Guid? NucleoId { get; init; }
    public Guid? SetorId { get; init; }
    public int? Mes { get; init; }
    public int? Ano { get; init; }
    public StatusEscala? Status { get; init; }
}
