using System.IO.Compression;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Entities;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Baixa de uma vez todas as escalas de uma consulta da lista (mesmos filtros, todas as páginas),
/// num .zip. Cada arquivo sai como "SIGLA-Mês-Ano" (ex.: "SCCV-Setembro-2026.pdf"); a resumida de
/// um núcleo é uma só para todos os setores dele e entra uma vez, com a sigla do núcleo.
/// </summary>
public class EscalaLoteExportService(
    IEscalaService escalaService,
    IEscalaPdfService escalaPdfService,
    IEscalaCsvService escalaCsvService,
    IEscalaResumidaService escalaResumidaService,
    IEscalaResumidaPdfService escalaResumidaPdfService) : IEscalaLoteExportService
{
    /// <summary>Teto de escalas por lote — evita uma requisição que gere centenas de PDFs.</summary>
    public const int MaximoEscalas = 300;

    public async Task<Result<(byte[] Content, string FileName)>> GerarAsync(
        EscalaListQuery query,
        string formato,
        string tipo,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        formato = (formato ?? string.Empty).Trim().ToLowerInvariant();
        tipo = (tipo ?? string.Empty).Trim().ToLowerInvariant();
        if (formato is not ("pdf" or "excel"))
        {
            return Result<(byte[], string)>.Failure("Formato inválido. Use PDF ou Excel.");
        }

        if (formato == "pdf" && tipo is not ("definitiva" or "resumida"))
        {
            return Result<(byte[], string)>.Failure("Tipo de PDF inválido. Use definitiva ou resumida.");
        }

        if (formato == "excel" && tipo != "resumida")
        {
            return Result<(byte[], string)>.Failure("Exportação em Excel completa ainda não implementada.");
        }

        var escalas = await ListarTodasAsync(query, actorLogin, cancellationToken);
        if (escalas.Count == 0)
        {
            return Result<(byte[], string)>.Failure("Nenhuma escala na lista para baixar.");
        }

        if (escalas.Count > MaximoEscalas)
        {
            return Result<(byte[], string)>.Failure(
                $"A lista tem {escalas.Count} escalas — filtre para no máximo {MaximoEscalas} antes de baixar.");
        }

        var nomesUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resumidasIncluidas = new HashSet<Guid>();
        var falhas = new List<string>();

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var escala in escalas)
            {
                var sigla = escala.SetorSigla ?? escala.NucleoSigla ?? "SEM-SIGLA";
                Result<(byte[] Content, string FileName)> arquivo;
                string extensao;

                if (formato == "excel")
                {
                    arquivo = await escalaCsvService.GenerateResumidoAsync(escala.Id, actorLogin, cancellationToken);
                    extensao = "xlsx";
                }
                else if (tipo == "definitiva")
                {
                    arquivo = await escalaPdfService.GenerateAsync(escala.Id, "horizontal", actorLogin, cancellationToken);
                    extensao = "pdf";
                }
                else
                {
                    // Resumida: só quem tem; a de núcleo é compartilhada e entra uma vez.
                    if (escala.ResumidaId is not Guid resumidaId || !resumidasIncluidas.Add(resumidaId))
                    {
                        continue;
                    }

                    var resumida = await escalaResumidaService.GetByIdAsync(resumidaId, actorLogin, cancellationToken);
                    sigla = resumida.Value?.SetorSigla ?? resumida.Value?.NucleoSigla ?? sigla;
                    arquivo = await escalaResumidaPdfService.GenerateAsync(resumidaId, actorLogin, cancellationToken);
                    extensao = "pdf";
                }

                if (!arquivo.Succeeded)
                {
                    falhas.Add($"{escala.Identificacao}: {arquivo.Error}");
                    continue;
                }

                var nome = NomeUnico($"{sigla}-{Escala.NomeMes(escala.Mes)}-{escala.Ano}", extensao, nomesUsados);
                var entrada = zip.CreateEntry(nome, CompressionLevel.Fastest);
                await using var destino = entrada.Open();
                await destino.WriteAsync(arquivo.Value.Content, cancellationToken);
            }

            if (falhas.Count > 0)
            {
                var entrada = zip.CreateEntry("nao-exportadas.txt", CompressionLevel.Fastest);
                await using var destino = new StreamWriter(entrada.Open());
                await destino.WriteAsync(string.Join(Environment.NewLine, falhas));
            }
        }

        if (nomesUsados.Count == 0)
        {
            return Result<(byte[], string)>.Failure(tipo == "resumida" && formato == "pdf"
                ? "Nenhuma das escalas listadas tem escala resumida."
                : "Não foi possível gerar nenhum arquivo.");
        }

        return Result<(byte[], string)>.Success((memoria.ToArray(), NomeDoZip(escalas)));
    }

    private async Task<List<EscalaListItemDto>> ListarTodasAsync(
        EscalaListQuery query,
        string actorLogin,
        CancellationToken cancellationToken)
    {
        var todas = new List<EscalaListItemDto>();
        for (var page = 1; ; page++)
        {
            var pagina = await escalaService.ListAsync(
                query with { Page = page, PageSize = 100 }, actorLogin, cancellationToken);
            todas.AddRange(pagina.Items);
            if (pagina.Items.Count == 0 || todas.Count >= pagina.TotalItems || todas.Count > MaximoEscalas)
            {
                return todas;
            }
        }
    }

    /// <summary>Duas escalas do mesmo setor no mês (versões antes de publicar) não podem ter o
    /// mesmo nome dentro do .zip: a segunda ganha " (2)", e assim por diante.</summary>
    private static string NomeUnico(string baseNome, string extensao, HashSet<string> usados)
    {
        var limpo = string.Concat(baseNome.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var nome = $"{limpo}.{extensao}";
        for (var i = 2; !usados.Add(nome); i++)
        {
            nome = $"{limpo} ({i}).{extensao}";
        }

        return nome;
    }

    private static string NomeDoZip(IReadOnlyList<EscalaListItemDto> escalas)
    {
        var periodos = escalas.Select(x => (x.Ano, x.Mes)).Distinct().ToList();
        return periodos.Count == 1
            ? $"Escalas-{Escala.NomeMes(periodos[0].Mes)}-{periodos[0].Ano}.zip"
            : "Escalas.zip";
    }
}
