using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// PDF da escala resumida — replica o formato reconhecido pela equipe (uma mini-tabela por
/// setor, lado a lado, cada uma com sua própria coluna de dia). Só a grade interativa web
/// (ver <c>escala-resumida-grid</c> no frontend) une tudo numa coluna de data só; aqui o
/// objetivo é impressão, então mantém o layout original.
/// </summary>
public class EscalaResumidaPdfService(
    IEscalaResumidaService escalaResumidaService,
    ApplicationDbContext db,
    IHostEnvironment env) : IEscalaResumidaPdfService
{
    private const string OrgaoTitulo = "POLÍCIA CIENTÍFICA DO RIO GRANDE DO NORTE";
    private const string OrgaoSubtitulo =
        "SIGAD IC - Sistema de Gestão Administrativa do Instituto de Criminalística";

    static EscalaResumidaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Result<(byte[] Content, string FileName)>> GenerateAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var detail = await escalaResumidaService.GetByIdAsync(id, actorLogin, cancellationToken);
        if (!detail.Succeeded)
        {
            return Result<(byte[], string)>.Failure(detail.Error!);
        }

        var escala = detail.Value!;
        var brasaoPci = LoadImageBytes("logo-pci-govrn-footer.png");
        var brasaoRn = LoadImageBytes("brasao-rn.png");
        var chefe = await ResolveChefeAsync(escala.SetorId, escala.NucleoId, cancellationToken);

        var bytes = Build(escala, brasaoPci, brasaoRn, chefe);
        var containerSigla = escala.SetorSigla ?? escala.NucleoSigla;
        var fileName = $"escala-resumida-{containerSigla}-{escala.Ano}-{escala.Mes:00}.pdf".Replace(" ", "-");

        return Result<(byte[], string)>.Success((bytes, fileName));
    }

    /// <summary>Chefia do setor — Diretor OU Subcoordenador se for a Direção IC (têm os mesmos
    /// poderes no sistema, qualquer um dos dois assina), chefia imediata nos demais setores —
    /// ou o chefe do núcleo, quando a escala resumida é de núcleo. `null` se não houver chefia
    /// cadastrada.</summary>
    private async Task<(string Nome, string Matricula, TipoChefia Tipo)?> ResolveChefeAsync(
        Guid? setorId, Guid? nucleoId, CancellationToken cancellationToken)
    {
        if (setorId is Guid s)
        {
            var sigla = await db.Setores
                .Where(x => x.Id == s)
                .Select(x => x.Sigla)
                .FirstOrDefaultAsync(cancellationToken);

            if (SetorSiglas.IsDirecaoIc(sigla))
            {
                var chefiaDirecao = await db.SetorChefias
                    .Where(x => x.SetorId == s
                        && (x.TipoChefia == TipoChefia.Diretor || x.TipoChefia == TipoChefia.Subcoordenador))
                    .OrderBy(x => x.TipoChefia)
                    .Select(x => new { x.Servidor.Nome, x.Servidor.Matricula, x.TipoChefia })
                    .FirstOrDefaultAsync(cancellationToken);
                return chefiaDirecao is null
                    ? null
                    : (chefiaDirecao.Nome, chefiaDirecao.Matricula, chefiaDirecao.TipoChefia);
            }

            var chefia = await db.SetorChefias
                .Where(x => x.SetorId == s && x.TipoChefia == TipoChefia.ChefiaImediata)
                .Select(x => new { x.Servidor.Nome, x.Servidor.Matricula })
                .FirstOrDefaultAsync(cancellationToken);
            return chefia is null ? null : (chefia.Nome, chefia.Matricula, TipoChefia.ChefiaImediata);
        }

        if (nucleoId is Guid n)
        {
            var chefe = await db.Nucleos
                .Where(x => x.Id == n && x.ChefeServidorId != null)
                .Select(x => new { x.ChefeServidor!.Nome, x.ChefeServidor.Matricula })
                .FirstOrDefaultAsync(cancellationToken);
            return chefe is null ? null : (chefe.Nome, chefe.Matricula, TipoChefia.ChefiaImediata);
        }

        return null;
    }

    private static void ComposeSignature(
        ColumnDescriptor col, (string Nome, string Matricula, TipoChefia Tipo)? chefe, string unidadeLabel)
    {
        col.Item().PaddingTop(16).AlignCenter().Column(sig =>
        {
            sig.Item().AlignCenter().Text(
                chefe is null ? "—" : $"{chefe.Value.Nome} - {chefe.Value.Matricula}").FontSize(9);
            sig.Item().AlignCenter().Text(TituloChefia(chefe, unidadeLabel)).FontSize(8);
        });
    }

    /// <summary>Diretor/Subcoordenador assinam como cargo institucional ("Diretor do Instituto
    /// de Criminalística"), não como "Chefe do {nome do setor Direção IC}" — os demais setores e
    /// núcleos continuam com o rótulo genérico de chefia.</summary>
    private static string TituloChefia((string Nome, string Matricula, TipoChefia Tipo)? chefe, string unidadeLabel) =>
        chefe?.Tipo switch
        {
            TipoChefia.Diretor => $"Diretor do {SetorSiglas.InstitutoNome}",
            TipoChefia.Subcoordenador => $"Subcoordenador do {SetorSiglas.InstitutoNome}",
            _ => $"Chefe do {unidadeLabel}",
        };

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

    private static byte[] Build(
        EscalaResumidaDetailDto escala,
        byte[]? brasaoPci,
        byte[]? brasaoRn,
        (string Nome, string Matricula, TipoChefia Tipo)? chefe)
    {
        var days = Enumerable
            .Range(0, escala.DataFim.DayNumber - escala.DataInicio.DayNumber + 1)
            .Select(i => escala.DataInicio.AddDays(i))
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Column(col =>
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
                    var containerLabel = escala.SetorId is not null
                        ? $"Setor: {escala.SetorNome} ({escala.SetorSigla})"
                        : $"Núcleo: {escala.NucleoNome} ({escala.NucleoSigla})";
                    col.Item().Text($"Ano: {escala.Ano}  |  Mês: {MesNome(escala.Mes)}  |  {containerLabel}");
                    col.Item().Text($"Gerado em {NowBrasil():dd/MM/yyyy HH:mm}  |  Status: {escala.Status}");
                    col.Item().PaddingTop(6);
                });

                page.Content().Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        foreach (var setor in escala.Setores.Where(s => s.Equipes.Count > 0))
                        {
                            row.RelativeItem().PaddingRight(6).Column(setorCol =>
                            {
                                var setorLabel = setor.SetorId is null
                                    ? EscalaResumidaSetor.AgentesLabel
                                    : $"{setor.SetorSigla} — {setor.SetorNome}";
                                setorCol.Item().Background(Colors.Grey.Lighten1).Padding(2)
                                    .Text(setorLabel).Bold().FontSize(7);
                                setorCol.Item().Table(table => BuildSetorTable(table, setor, days));
                            });
                        }
                    });

                    content.Item().Column(sig => ComposeSignature(
                        sig, chefe, escala.SetorNome ?? escala.NucleoNome ?? "—"));
                });

                page.Footer().PaddingTop(8).Text("DO = Diária Operacional disponível.");
            });
        }).GeneratePdf();
    }

    private static void BuildSetorTable(TableDescriptor table, EscalaResumidaSetorDto setor, List<DateOnly> days)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.ConstantColumn(46);
            foreach (var _ in setor.Equipes)
            {
                columns.RelativeColumn();
            }
        });

        table.Header(header =>
        {
            header.Cell().Element(HeaderCell).Text("Dia");
            foreach (var equipe in setor.Equipes)
            {
                header.Cell().Element(HeaderCell).AlignCenter().Text(equipe.Nome);
            }
        });

        foreach (var day in days)
        {
            var weekend = IsWeekend(day);
            table.Cell().Element(c => DayCell(c, weekend)).Text($"{DayLetter(day)} {day.Day:00}");
            foreach (var equipe in setor.Equipes)
            {
                var dia = equipe.Dias.FirstOrDefault(x => x.Data == day);
                var rotulo = dia?.Rotulo ?? "";
                table.Cell().Element(c => DayCell(c, weekend, rotulo)).AlignCenter().Text(rotulo);
            }
        }
    }

    /// <summary>O servidor roda em UTC (container Docker) — Brasil (RN) é sempre UTC-3, sem
    /// horário de verão desde 2019, então um offset fixo é mais robusto do que depender do
    /// tz database do ambiente (IANA/Windows).</summary>
    private static DateTime NowBrasil() => DateTime.UtcNow.AddHours(-3);

    private static bool IsWeekend(DateOnly d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

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

    private static IContainer HeaderCell(IContainer c) =>
        c.Border(0.5f).Background(Colors.Grey.Lighten2).Padding(2);

    private static IContainer DayCell(IContainer c, bool weekend, string? rotulo = null)
    {
        var bg = string.Equals(rotulo, "DO", StringComparison.OrdinalIgnoreCase)
            ? Colors.Red.Lighten4
            : weekend ? Colors.Amber.Lighten4 : Colors.White;
        return c.Border(0.5f).Background(bg).Padding(1);
    }
}
