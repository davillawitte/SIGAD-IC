using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;

namespace TemplateSistema.Application.Abstractions;

/// <summary>Exportação da listagem de servidores em PDF ou Excel, sempre em ordem alfabética.</summary>
public interface IServidorExportService
{
    Task<Result<(byte[] Content, string FileName, string ContentType)>> GerarAsync(
        ExportarServidoresRequest request,
        CancellationToken cancellationToken = default);
}
