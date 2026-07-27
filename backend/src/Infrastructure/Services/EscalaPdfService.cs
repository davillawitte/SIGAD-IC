using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

public class EscalaPdfService(
    ApplicationDbContext db,
    IEscalaService escalaService,
    IHostEnvironment env) : IEscalaPdfService
{
    private const string OrgaoTitulo = "POLÍCIA CIENTÍFICA DO RIO GRANDE DO NORTE";
    private const string OrgaoSubtitulo =
        "SIGAD IC - Sistema de Gestão Administrativa do Instituto de Criminalística";

    static EscalaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Result<(byte[] Content, string FileName)>> GenerateAsync(
        Guid id,
        string layout,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var detail = await escalaService.GetByIdAsync(id, actorLogin, cancellationToken);
        if (!detail.Succeeded)
        {
            return Result<(byte[], string)>.Failure(detail.Error!);
        }

        // Só tenta sincronizar afastamentos em escala editável; se o ator não puder
        // mutar (ex.: visão institucional), gera o PDF com o snapshot atual.
        if (detail.Value!.Status is not (StatusEscala.Publicada or StatusEscala.DevolucaoSolicitada))
        {
            var applied = await escalaService.AplicarAfastamentosAsync(id, actorLogin, cancellationToken);
            if (applied.Succeeded)
            {
                detail = await escalaService.GetByIdAsync(id, actorLogin, cancellationToken);
                if (!detail.Succeeded)
                {
                    return Result<(byte[], string)>.Failure(detail.Error!);
                }
            }
            else if (!IsMutationDenied(applied.Error))
            {
                return Result<(byte[], string)>.Failure(applied.Error!);
            }
        }

        var escala = detail.Value!;
        var tipos = await db.TiposOcorrencia.AsNoTracking()
            .ToDictionaryAsync(x => x.Codigo, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var brasaoPci = LoadImageBytes("logo-pci-govrn-footer.png");
        var brasaoRn = LoadImageBytes("brasao-rn.png");
        var isHorizontal = !string.Equals(layout, "vertical", StringComparison.OrdinalIgnoreCase);

        var bytes = isHorizontal
            ? BuildHorizontal(escala, tipos, brasaoPci, brasaoRn)
            : BuildVertical(escala, tipos, brasaoPci, brasaoRn);

        var suffix = isHorizontal ? "horizontal" : "vertical";
        var fileName = $"escala-{escala.SetorSigla}-{escala.Ano}-{escala.Mes:00}-{suffix}.pdf"
            .Replace(" ", "-");

        return Result<(byte[], string)>.Success((bytes, fileName));
    }

    private static bool IsMutationDenied(string? error) =>
        !string.IsNullOrWhiteSpace(error)
        && (error.Contains("Sem permissão", StringComparison.OrdinalIgnoreCase)
            || error.Contains("só pode ser alterada", StringComparison.OrdinalIgnoreCase));

    private byte[]? LoadImageBytes(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(env.ContentRootPath, "wwwroot", "assets", "images", fileName),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "images", fileName),
            Path.Combine(AppContext.BaseDirectory, "assets", "images", fileName),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
        }

        return null;
    }

    private static void ComposeHeader(
        ColumnDescriptor col,
        EscalaDetailDto escala,
        byte[]? brasaoPci,
        byte[]? brasaoRn,
        Func<IEnumerable<string>> cargosFn)
    {
        col.Item().Row(row =>
        {
            if (brasaoPci is { Length: > 0 })
            {
                row.ConstantItem(48).Height(48).Image(brasaoPci).FitArea();
            }
            else
            {
                row.ConstantItem(48);
            }

            row.RelativeItem().AlignMiddle().AlignCenter().Column(center =>
            {
                center.Item().AlignCenter().Text(OrgaoTitulo).FontSize(10).SemiBold();
                center.Item().AlignCenter().Text(OrgaoSubtitulo).FontSize(7);
            });

            if (brasaoRn is { Length: > 0 })
            {
                row.ConstantItem(48).Height(48).Image(brasaoRn).FitArea();
            }
            else
            {
                row.ConstantItem(48);
            }
        });

        col.Item().PaddingTop(4).Text(escala.Identificacao.ToUpperInvariant()).Bold().FontSize(12);
        var cargos = string.Join(", ", cargosFn().Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
        col.Item().Text(
            $"Ano: {escala.Ano}  |  Mês: {MesNome(escala.Mes)}  |  Instituto/Regional: IC/Natal");
        col.Item().Text(
            $"Setor/Núcleo: {escala.SetorNome} ({escala.SetorSigla})  |  " +
            $"Responsável: {escala.CreatedBy ?? "—"}");
        col.Item().Text($"Cargos: {(string.IsNullOrWhiteSpace(cargos) ? "—" : cargos)}");
        col.Item().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}  |  Status: {escala.Status}");
        col.Item().PaddingTop(6);
    }

    private static byte[] BuildHorizontal(
        EscalaDetailDto escala,
        Dictionary<string, Domain.Entities.TipoOcorrencia> tipos,
        byte[]? brasaoPci,
        byte[]? brasaoRn)
    {
        var days = Enumerable
            .Range(0, escala.DataFim.DayNumber - escala.DataInicio.DayNumber + 1)
            .Select(i => escala.DataInicio.AddDays(i))
            .ToList();

        var weekLetters = days.Select(DayLetter).ToList();
        var servidores = escala.Servidores.OrderBy(x => x.Ordem).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Column(col =>
                    ComposeHeader(
                        col,
                        escala,
                        brasaoPci,
                        brasaoRn,
                        () => escala.Servidores.Select(s =>
                            string.IsNullOrWhiteSpace(s.CargoCodigo) ? s.CargoNome : s.CargoCodigo)));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(52);
                        columns.RelativeColumn(2.2f);
                        columns.ConstantColumn(36);
                        foreach (var _ in days)
                        {
                            columns.ConstantColumn(16);
                        }

                        columns.ConstantColumn(28);
                        columns.ConstantColumn(28);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Matrícula");
                        header.Cell().Element(HeaderCell).Text("Nome do Servidor");
                        header.Cell().Element(HeaderCell).Text("Cargo");
                        for (var i = 0; i < days.Count; i++)
                        {
                            var weekend = IsWeekend(days[i]);
                            header.Cell().Element(c => DayHeaderCell(c, weekend)).AlignCenter()
                                .Text($"{days[i].Day}\n{weekLetters[i]}");
                        }

                        header.Cell().Element(HeaderCell).AlignCenter().Text("CH Pres.");
                        header.Cell().Element(HeaderCell).AlignCenter().Text("CH Rem.");
                    });

                    foreach (var s in servidores)
                    {
                        var map = s.Ocorrencias.ToDictionary(o => o.Data);
                        var chPres = s.Ocorrencias
                            .Where(o => IsPresencial(o.TipoOcorrenciaCodigo))
                            .Sum(o => o.Horas ?? tipos.GetValueOrDefault(o.TipoOcorrenciaCodigo)?.HorasPadrao ?? 0);
                        var chRem = s.Ocorrencias
                            .Where(o => o.TipoOcorrenciaCodigo.StartsWith("TL", StringComparison.OrdinalIgnoreCase))
                            .Sum(o => o.Horas ?? tipos.GetValueOrDefault(o.TipoOcorrenciaCodigo)?.HorasPadrao ?? 0);

                        table.Cell().Element(BodyCell).Text(s.Matricula);
                        table.Cell().Element(BodyCell).Text(s.ServidorNome);
                        table.Cell().Element(BodyCell).Text(
                            string.IsNullOrWhiteSpace(s.CargoCodigo) ? (s.CargoNome ?? "—") : s.CargoCodigo);

                        foreach (var day in days)
                        {
                            var weekend = IsWeekend(day);
                            var codigo = map.TryGetValue(day, out var oc) ? oc.TipoOcorrenciaCodigo : "";
                            table.Cell().Element(c => DayBodyCell(c, weekend, codigo)).AlignCenter().Text(codigo);
                        }

                        table.Cell().Element(BodyCell).AlignCenter().Text($"{chPres:0}");
                        table.Cell().Element(BodyCell).AlignCenter().Text($"{chRem:0}");
                    }
                });

                page.Footer().PaddingTop(8).Text(text =>
                {
                    text.Span("Legenda: ");
                    text.Span(string.Join(" | ", tipos.Values.OrderBy(t => t.Codigo).Select(t => $"{t.Codigo}={t.Nome}")));
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildVertical(
        EscalaDetailDto escala,
        Dictionary<string, Domain.Entities.TipoOcorrencia> tipos,
        byte[]? brasaoPci,
        byte[]? brasaoRn)
    {
        var days = Enumerable
            .Range(0, escala.DataFim.DayNumber - escala.DataInicio.DayNumber + 1)
            .Select(i => escala.DataInicio.AddDays(i))
            .ToList();
        var servidores = escala.Servidores.OrderBy(x => x.Ordem).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4); // portrait (orientação vertical)
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                    ComposeHeader(
                        col,
                        escala,
                        brasaoPci,
                        brasaoRn,
                        () => escala.Servidores.Select(s =>
                            string.IsNullOrWhiteSpace(s.CargoCodigo) ? s.CargoNome : s.CargoCodigo)));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        foreach (var _ in servidores)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Data");
                        foreach (var s in servidores)
                        {
                            var cargo = string.IsNullOrWhiteSpace(s.CargoCodigo) ? s.CargoNome : s.CargoCodigo;
                            header.Cell().Element(HeaderCell)
                                .Text($"{cargo} — {s.ServidorNome} — Mat. {s.Matricula}");
                        }
                    });

                    foreach (var day in days)
                    {
                        table.Cell().Element(BodyCell).Text(FormatVerticalDate(day));
                        foreach (var s in servidores)
                        {
                            var oc = s.Ocorrencias.FirstOrDefault(o => o.Data == day);
                            var codigo = oc?.TipoOcorrenciaCodigo ?? "—";
                            var weekend = IsWeekend(day);
                            table.Cell().Element(c => DayBodyCell(c, weekend, oc?.TipoOcorrenciaCodigo ?? ""))
                                .Text(codigo);
                        }
                    }
                });
            });
        }).GeneratePdf();
    }

    private static bool IsPresencial(string codigo) =>
        !codigo.StartsWith("TL", StringComparison.OrdinalIgnoreCase)
        && codigo is not ("D" or "F" or "FR" or "LP" or "LM" or "LO" or "R");

    private static bool IsWeekend(DateOnly d) =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static string FormatVerticalDate(DateOnly d) =>
        $"{DayLetter(d)} - {d.Day:00}/{MesAbrev(d.Month)}";

    private static string DayLetter(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Sunday => "D",
        DayOfWeek.Monday => "S",
        DayOfWeek.Tuesday => "T",
        DayOfWeek.Wednesday => "Q",
        DayOfWeek.Thursday => "Q",
        DayOfWeek.Friday => "S",
        DayOfWeek.Saturday => "S",
        _ => "?",
    };

    private static string MesNome(int mes) => mes switch
    {
        1 => "Janeiro", 2 => "Fevereiro", 3 => "Março", 4 => "Abril",
        5 => "Maio", 6 => "Junho", 7 => "Julho", 8 => "Agosto",
        9 => "Setembro", 10 => "Outubro", 11 => "Novembro", 12 => "Dezembro",
        _ => mes.ToString(),
    };

    private static string MesAbrev(int mes) => mes switch
    {
        1 => "Jan", 2 => "Fev", 3 => "Mar", 4 => "Abr",
        5 => "Mai", 6 => "Jun", 7 => "Jul", 8 => "Ago",
        9 => "Set", 10 => "Out", 11 => "Nov", 12 => "Dez",
        _ => mes.ToString(),
    };

    private static IContainer HeaderCell(IContainer c) =>
        c.Border(0.5f).Background(Colors.Grey.Lighten2).Padding(2);

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.5f).Padding(2);

    private static IContainer DayHeaderCell(IContainer c, bool weekend) =>
        c.Border(0.5f).Background(weekend ? Colors.Amber.Lighten3 : Colors.Grey.Lighten2).Padding(1);

    private static IContainer DayBodyCell(IContainer c, bool weekend, string codigo)
    {
        var bg = CellBackground(codigo, weekend);
        return c.Border(0.5f).Background(bg).Padding(1);
    }

    private static string CellBackground(string codigo, bool weekend)
    {
        if (string.Equals(codigo, "LM", StringComparison.OrdinalIgnoreCase))
        {
            return Colors.Red.Lighten4;
        }

        if (string.Equals(codigo, "FR", StringComparison.OrdinalIgnoreCase))
        {
            return Colors.Blue.Lighten4;
        }

        return weekend ? Colors.Amber.Lighten4 : Colors.White;
    }
}
