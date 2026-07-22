namespace TemplateSistema.Application.Nucleos;

public record NucleoListItemDto(
    Guid Id,
    string Nome,
    string Sigla,
    Guid? ChefeServidorId,
    string? ChefeNome,
    int QuantidadeSetores);

public record NucleoDetailDto(
    Guid Id,
    string Nome,
    string Sigla,
    Guid? ChefeServidorId,
    string? ChefeNome,
    IReadOnlyList<Guid> SetorIds);

public record CreateNucleoRequest(string Nome, string Sigla, Guid? ChefeServidorId);

public record UpdateNucleoRequest(string Nome, string Sigla, Guid? ChefeServidorId);
