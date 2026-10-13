using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Permutas;

public record PermutaDto(
    Guid Id,
    Guid SolicitanteServidorId,
    string SolicitanteNome,
    string SolicitanteMatricula,
    Guid PermutanteServidorId,
    string PermutanteNome,
    string PermutanteMatricula,
    DateOnly DataIda,
    DateOnly DataVolta,
    string? Observacao,
    string? CreatedBy,
    DateTime CreatedAt,
    // Nome do servidor de quem registrou (o login é o CPF).
    string? RegistradoPorNome = null);

/// <summary><c>DataIda</c>: dia do solicitante que passa ao permutante. <c>DataVolta</c>: dia do
/// permutante que volta ao solicitante.</summary>
public record CreatePermutaRequest(
    Guid SolicitanteServidorId,
    Guid PermutanteServidorId,
    DateOnly DataIda,
    DateOnly DataVolta,
    string? Observacao = null);

public record PermutaListQuery(int? Ano = null, int? Mes = null, Guid? ServidorId = null);

public record PermutaServidorOpcaoDto(Guid Id, string Nome, string Matricula, string? Lotacao);

/// <summary>Solicitante e permutante: servidores da chefia do usuário — chefe de núcleo vê todos
/// os setores do núcleo (mesmo recorte da tela de Afastamentos).</summary>
public record PermutaServidoresDto(
    IReadOnlyList<PermutaServidorOpcaoDto> Solicitantes,
    IReadOnlyList<PermutaServidorOpcaoDto> Permutantes);

public record PermutaDiaDto(DateOnly Data, string? TipoOcorrenciaCodigo, string? TipoOcorrenciaNome, bool Permuta);

/// <summary>Dias do servidor no mês, da escala dele (publicada &gt; finalizada &gt; rascunho). Sem
/// escala no mês: <c>EscalaId</c> nulo e dias sem código.</summary>
public record PermutaEscalaServidorDto(
    Guid? EscalaId,
    string? EscalaIdentificacao,
    StatusEscala? Status,
    IReadOnlyList<PermutaDiaDto> Dias,
    string? LotacaoNome = null);
