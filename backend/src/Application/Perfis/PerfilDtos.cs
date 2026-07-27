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
    IReadOnlyDictionary<string, Abrangencia> AbrangenciaPorPermissao,
    /// <summary>Áreas de acesso detectadas (Gestão do Setor / Institucional / Admin).</summary>
    IReadOnlyList<string> Areas);

public record CreatePerfilRequest(
    string Nome,
    string? Codigo,
    string? Descricao,
    IReadOnlyList<Guid>? PermissaoIds = null,
    IReadOnlyDictionary<string, Abrangencia>? AbrangenciaPorPermissao = null,
    IReadOnlyList<string>? Areas = null);

public record UpdatePerfilRequest(
    string Nome,
    string? Descricao,
    bool? Ativo);

/// <summary>
/// Preferir <see cref="Areas"/>: libera o módulo inteiro. Se informado, expandido no servidor.
/// </summary>
public record SetPerfilPermissoesRequest(
    IReadOnlyList<Guid>? PermissaoIds = null,
    IReadOnlyDictionary<string, Abrangencia>? AbrangenciaPorPermissao = null,
    IReadOnlyList<string>? Areas = null);

public record PerfilExclusaoImpactoDto(
    int QuantidadeUsuarios,
    bool TemUsuariosVinculados);

public record DesativarPerfilRequest(Guid? PerfilSubstitutoId, bool RemoverVinculosSemSubstituto = false);
