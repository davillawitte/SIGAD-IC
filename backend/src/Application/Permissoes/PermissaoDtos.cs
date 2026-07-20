namespace TemplateSistema.Application.Permissoes;

public record PermissaoDto(
    Guid Id,
    string Codigo,
    string Nome,
    string? Descricao,
    string Modulo,
    bool Sistema,
    bool Ativo);

public record CreatePermissaoRequest(
    string Codigo,
    string Nome,
    string Modulo,
    string? Descricao);

public record UpdatePermissaoRequest(
    string Nome,
    string Modulo,
    string? Descricao,
    bool? Ativo);
