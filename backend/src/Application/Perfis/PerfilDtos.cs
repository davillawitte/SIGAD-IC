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
    IReadOnlyList<string> Permissoes);

public record CreatePerfilRequest(
    string Nome,
    string? Codigo,
    string? Descricao,
    IReadOnlyList<Guid>? PermissaoIds);

public record UpdatePerfilRequest(
    string Nome,
    string? Descricao,
    bool? Ativo);

public record SetPerfilPermissoesRequest(IReadOnlyList<Guid> PermissaoIds);

public record PerfilExclusaoImpactoDto(
    int QuantidadeUsuarios,
    bool TemUsuariosVinculados);

public record DesativarPerfilRequest(Guid? PerfilSubstitutoId, bool RemoverVinculosSemSubstituto = false);
