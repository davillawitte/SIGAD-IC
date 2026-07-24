using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Setores;

public record SetorChefiaDto(TipoChefia TipoChefia, Guid ServidorId, string? ServidorNome);

public record SetorChefiaInput(TipoChefia TipoChefia, Guid ServidorId);

public record SetorListItemDto(
    Guid Id,
    string Nome,
    string Sigla,
    string? Resumo,
    Guid? NucleoId,
    string? NucleoNome,
    bool IsDirecaoIc,
    IReadOnlyList<SetorChefiaDto> Chefias);

public record ChefiaConflitoDto(
    Guid ServidorId,
    string ServidorNome,
    TipoChefia TipoChefia,
    Guid SetorId,
    string SetorNome);

public record PreviewChefiasConflitosRequest(
    Guid? SetorId,
    IReadOnlyList<SetorChefiaInput> Chefias);

public record CreateSetorRequest(
    string Nome,
    string Sigla,
    string? Resumo,
    Guid? NucleoId,
    IReadOnlyList<SetorChefiaInput> Chefias,
    bool ConfirmarRemocaoChefiasEmOutrosSetores = false);

public record UpdateSetorRequest(
    string Nome,
    string Sigla,
    string? Resumo,
    Guid? NucleoId,
    IReadOnlyList<SetorChefiaInput> Chefias,
    bool ConfirmarRemocaoChefiasEmOutrosSetores = false);

public record EstruturaOrganizacionalDto(
    IReadOnlyList<NucleoComSetoresDto> Nucleos,
    SetorListItemDto? DirecaoIc);

public record NucleoComSetoresDto(
    Guid Id,
    string Nome,
    string Sigla,
    Guid? ChefeServidorId,
    string? ChefeNome,
    IReadOnlyList<SetorListItemDto> Setores);
