using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Perfis;

public record PerfilListItemDto(
    Guid Id,
    string Nome,
    string Codigo,
    string? Descricao,
    bool Sistema,
    bool Ativo,
    int QuantidadePermissoes);

public record PerfilDetailDto(
    Guid Id,
    string Nome,
    string Codigo,
    string? Descricao,
    bool Sistema,
    bool Ativo,
    IReadOnlyList<Guid> PermissaoIds,
    IReadOnlyList<string> Permissoes,
    /// <summary>Abrangência por código de permissão (ex.: escalas.listar → TodosOsSetores).</summary>
    IReadOnlyDictionary<string, Abrangencia> AbrangenciaPorPermissao);

public record CreatePerfilRequest(
    string Nome,
    string? Codigo,
    string? Descricao,
    IReadOnlyList<Guid>? PermissaoIds,
    IReadOnlyDictionary<string, Abrangencia>? AbrangenciaPorPermissao = null);

public record UpdatePerfilRequest(
    string Nome,
    string? Descricao,
    bool? Ativo);

/// <summary>
/// Permissões ausentes em <paramref name="AbrangenciaPorPermissao"/> usam
/// <see cref="Abrangencia.MeusSetores"/>. Chaves = código da permissão.
/// </summary>
public record SetPerfilPermissoesRequest(
    IReadOnlyList<Guid> PermissaoIds,
    IReadOnlyDictionary<string, Abrangencia>? AbrangenciaPorPermissao = null);

public record PerfilExclusaoImpactoDto(
    int QuantidadeUsuarios,
    bool TemUsuariosVinculados);

public record DesativarPerfilRequest(Guid? PerfilSubstitutoId, bool RemoverVinculosSemSubstituto = false);
