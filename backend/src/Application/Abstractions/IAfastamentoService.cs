using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Application.Abstractions;

public interface IAfastamentoService
{
    Task<IReadOnlyList<AfastamentoDto>> ListAsync(
        AfastamentoListQuery query,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> GetByIdAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> CreateAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    /// <summary>Gestão Institucional: cadastra afastamento para qualquer servidor, exceto os
    /// lotados na Direção do IC (mesmo recorte da listagem institucional).</summary>
    Task<Result<AfastamentoDto>> CreateInstitucionalAsync(
        CreateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    /// <summary>Servidores que a Gestão Institucional pode escolher no cadastro de afastamento.</summary>
    Task<IReadOnlyList<AfastamentoServidorOpcaoDto>> ListServidoresInstitucionaisAsync(
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result<AfastamentoDto>> UpdateAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);

    /// <summary>Gestão Institucional: altera afastamento de qualquer servidor, exceto da Direção do IC.</summary>
    Task<Result<AfastamentoDto>> UpdateInstitucionalAsync(
        Guid id,
        UpdateAfastamentoRequest request,
        string actorLogin,
        CancellationToken cancellationToken = default);

    /// <summary>Gestão Institucional: exclui afastamento de qualquer servidor, exceto da Direção do IC.</summary>
    Task<Result> DeleteInstitucionalAsync(Guid id, string actorLogin, CancellationToken cancellationToken = default);
}
