using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Exporta a listagem de servidores (tela Servidores) em PDF ou Excel. Sempre em ordem
/// alfabética de nome; separado por setor, os grupos também vêm em ordem alfabética de lotação
/// ("Sem lotação" por último). Mesmos dados da listagem — sem CPF.
/// </summary>
public class ServidorExportService(ApplicationDbContext db, IHostEnvironment env) : IServidorExportService
{
    public const string SemLotacao = "Sem lotação";
    private const string OrgaoTitulo = "POLÍCIA CIENTÍFICA DO RIO GRANDE DO NORTE";
    private const string OrgaoSubtitulo =
        "SIGAD IC - Sistema de Gestão Administrativa do Instituto de Criminalística";

    private static readonly CultureInfo PtBr = new("pt-BR");
    private static readonly StringComparer Alfabetica = StringComparer.Create(PtBr, ignoreCase: true);

    static ServidorExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Uma linha da exportação.</summary>
    public sealed record Linha(string Nome, string Matricula, string Cargo, string Lotacao, string Email, string Situacao);

    public async Task<Result<(byte[] Content, string FileName, string ContentType)>> GerarAsync(
        ExportarServidoresRequest request,
        CancellationToken cancellationToken = default)
    {
        var formato = (request.Formato ?? string.Empty).Trim().ToLowerInvariant();
        if (formato is not ("pdf" or "excel"))
        {
            return Result<(byte[], string, string)>.Failure("Formato inválido. Use PDF ou Excel.");
        }

        var filtrada = request.ServidorIds is { Count: > 0 };
        var linhas = await CarregarLinhasAsync(request.ServidorIds, cancellationToken);
        if (linhas.Count == 0)
        {
            return Result<(byte[], string, string)>.Failure("Nenhum servidor para exportar.");
        }

        var grupos = request.AgruparPorSetor
            ? Agrupar(linhas)
            : [(Lotacao: (string?)null, Linhas: linhas)];
        var descricao = $"{(filtrada ? "Listagem filtrada" : "Todos os servidores")} — {linhas.Count} "
            + (linhas.Count == 1 ? "servidor" : "servidores")
            + (request.AgruparPorSetor ? " — separados por setor" : string.Empty);
        var hoje = DateTime.UtcNow.AddHours(-3);

        return formato == "pdf"
            ? Result<(byte[], string, string)>.Success((
                GerarPdf(grupos, descricao, hoje, request.AgruparPorSetor),
                $"Servidores-{hoje:yyyy-MM-dd}.pdf",
                "application/pdf"))
            : Result<(byte[], string, string)>.Success((
                GerarExcel(grupos, descricao, hoje, request.AgruparPorSetor),
                $"Servidores-{hoje:yyyy-MM-dd}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }

    /// <summary>Servidores em ordem alfabética (pt-BR), limitados aos ids quando informados.</summary>
    public async Task<List<Linha>> CarregarLinhasAsync(
        IReadOnlyList<Guid>? servidorIds,
        CancellationToken cancellationToken)
    {
        var query = db.Servidores
            .AsNoTracking()
            .Include(x => x.Cargo)
            .Include(x => x.Setor)
            .Include(x => x.Nucleo)
            .AsQueryable();

        if (servidorIds is { Count: > 0 })
        {
            var ids = servidorIds.Distinct().ToList();
            query = query.Where(x => ids.Contains(x.Id));
        }

        var servidores = await query.ToListAsync(cancellationToken);
        return servidores
            .Select(s => new Linha(
                s.Nome,
                s.Matricula,
                s.Cargo?.Nome ?? string.Empty,
                s.Setor is not null
                    ? $"{s.Setor.Sigla} — {s.Setor.Nome}"
                    : s.Nucleo is not null
                        ? $"{s.Nucleo.Sigla} — {s.Nucleo.Nome} (núcleo)"
                        : SemLotacao,
                s.Email ?? string.Empty,
                s.Status.ToString()))
            .OrderBy(x => x.Nome, Alfabetica)
            .ToList();
    }

    /// <summary>Grupos por lotação em ordem alfabética, "Sem lotação" por último; dentro de cada
    /// grupo a ordem alfabética de nome é mantida.</summary>
    public static List<(string? Lotacao, List<Linha> Linhas)> Agrupar(IEnumerable<Linha> linhas) =>
        linhas
            .GroupBy(x => x.Lotacao)
            .OrderBy(g => g.Key == SemLotacao ? 1 : 0)
            .ThenBy(g => g.Key, Alfabetica)
            .Select(g => ((string?)g.Key, g.OrderBy(x => x.Nome, Alfabetica).ToList()))
            .ToList();

    private byte[] GerarPdf(
        IReadOnlyList<(string? Lotacao, List<Linha> Linhas)> grupos,
        string descricao,
        DateTime geradoEm,
        bool agrupado)
    {
        var brasaoPci = PdfAssets.LoadImage(env, PdfAssets.BrasaoPci);
        var brasaoRn = PdfAssets.LoadImage(env, PdfAssets.BrasaoRn);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (brasaoPci is { Length: > 0 })
                        {
                            row.ConstantItem(44).Height(44).Image(brasaoPci).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(44);
                        }

                        row.RelativeItem().AlignMiddle().AlignCenter().Column(center =>
                        {
                            center.Item().AlignCenter().Text(OrgaoTitulo).FontSize(10).SemiBold();
                            center.Item().AlignCenter().Text(OrgaoSubtitulo).FontSize(7);
                        });

                        if (brasaoRn is { Length: > 0 })
                        {
                            row.ConstantItem(44).Height(44).Image(brasaoRn).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(44);
                        }
                    });

                    col.Item().PaddingTop(4).Text("SERVIDORES").Bold().FontSize(12);
                    col.Item().Text($"{descricao}  |  Gerado em {geradoEm:dd/MM/yyyy HH:mm}");
                    col.Item().PaddingTop(6);
                });

                page.Content().Column(content =>
                {
                    foreach (var (lotacao, linhas) in grupos)
                    {
                        if (lotacao is not null)
                        {
                            content.Item().PaddingTop(6).Background(Colors.Grey.Lighten2).Padding(3)
                                .Text($"{lotacao} ({linhas.Count})").Bold();
                        }

                        content.Item().Table(table => MontarTabela(table, linhas, mostrarLotacao: !agrupado));
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void MontarTabela(TableDescriptor table, IReadOnlyList<Linha> linhas, bool mostrarLotacao)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);
            columns.RelativeColumn(1.2f);
            columns.RelativeColumn(2);
            if (mostrarLotacao)
            {
                columns.RelativeColumn(2.6f);
            }

            columns.RelativeColumn(2.4f);
            columns.RelativeColumn(1);
        });

        table.Header(header =>
        {
            foreach (var titulo in Cabecalho(mostrarLotacao))
            {
                header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3).Text(titulo).SemiBold();
            }
        });

        foreach (var linha in linhas)
        {
            foreach (var valor in Valores(linha, mostrarLotacao))
            {
                table.Cell().Border(0.5f).Padding(3).Text(valor);
            }
        }
    }

    private static byte[] GerarExcel(
        IReadOnlyList<(string? Lotacao, List<Linha> Linhas)> grupos,
        string descricao,
        DateTime geradoEm,
        bool agrupado)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Servidores");
        var cabecalho = Cabecalho(!agrupado).ToList();

        ws.Cell(1, 1).Value = "Servidores";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"{descricao}  |  Gerado em {geradoEm:dd/MM/yyyy HH:mm}";

        var linhaAtual = 4;
        if (!agrupado)
        {
            EscreverCabecalho(ws, linhaAtual, cabecalho);
            var inicio = linhaAtual;
            linhaAtual++;
            foreach (var linha in grupos[0].Linhas)
            {
                EscreverValores(ws, linhaAtual++, Valores(linha, mostrarLotacao: true));
            }

            ws.Range(inicio, 1, linhaAtual - 1, cabecalho.Count).SetAutoFilter();
        }
        else
        {
            foreach (var (lotacao, linhas) in grupos)
            {
                var titulo = ws.Range(linhaAtual, 1, linhaAtual, cabecalho.Count).Merge();
                titulo.Value = $"{lotacao} ({linhas.Count})";
                titulo.Style.Font.Bold = true;
                titulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
                linhaAtual++;
                EscreverCabecalho(ws, linhaAtual++, cabecalho);
                foreach (var linha in linhas)
                {
                    EscreverValores(ws, linhaAtual++, Valores(linha, mostrarLotacao: false));
                }

                linhaAtual++;
            }
        }

        ws.SheetView.FreezeRows(agrupado ? 0 : 4);
        ws.Columns(1, cabecalho.Count).AdjustToContents(4, linhaAtual);
        foreach (var coluna in ws.Columns(1, cabecalho.Count))
        {
            if (coluna.Width > 60)
            {
                coluna.Width = 60;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void EscreverCabecalho(IXLWorksheet ws, int linha, IReadOnlyList<string> titulos)
    {
        for (var i = 0; i < titulos.Count; i++)
        {
            var cell = ws.Cell(linha, i + 1);
            cell.Value = titulos[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEDED");
        }
    }

    private static void EscreverValores(IXLWorksheet ws, int linha, IEnumerable<string> valores)
    {
        var coluna = 1;
        foreach (var valor in valores)
        {
            ws.Cell(linha, coluna++).Value = valor;
        }
    }

    private static IEnumerable<string> Cabecalho(bool mostrarLotacao)
    {
        yield return "Nome";
        yield return "Matrícula";
        yield return "Cargo";
        if (mostrarLotacao)
        {
            yield return "Setor/Núcleo";
        }

        yield return "E-mail";
        yield return "Situação";
    }

    private static IEnumerable<string> Valores(Linha linha, bool mostrarLotacao)
    {
        yield return linha.Nome;
        yield return linha.Matricula;
        yield return linha.Cargo;
        if (mostrarLotacao)
        {
            yield return linha.Lotacao;
        }

        yield return linha.Email;
        yield return linha.Situacao;
    }
}
