using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permutas;

namespace TemplateSistema.Application.Abstractions;

public interface IPermutaService
{
    Task<IReadOnlyList<PermutaDto>> ListAsync(
        PermutaListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<PermutaDto>> CreateAsync(
        CreatePermutaRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);

    Task<PermutaServidoresDto> ListServidoresAsync(string actorLogin, CancellationToken cancellationToken = default);

    Task<PermutaEscalaServidorDto> GetEscalaServidorAsync(
        Guid servidorId,
        int ano,
        int mes,
        CancellationToken cancellationToken = default);
}
