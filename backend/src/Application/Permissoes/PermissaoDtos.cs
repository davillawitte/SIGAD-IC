namespace TemplateSistema.Application.Permissoes;

public record PermissaoDto(
    Guid Id,
    string Codigo,
    string Nome,
    string? Descricao,
    string Modulo,
    string Area,
    bool Sistema,
    bool Ativo);
