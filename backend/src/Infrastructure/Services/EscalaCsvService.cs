using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Persistence;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Gera o Excel "resumido" (auxílio-alimentação) de uma escala: por servidor, cargo/matrícula/
/// núcleo/setor, quantidade e valor do auxílio (cada plantão de 24h vale 3 unidades, cada
/// plantão de 12h vale 2 — R$ 20,00 cada), e a sequência cronológica dos dias de plantão do mês,
/// com férias/licenças (FR/LM/LO/LP) ocupando uma posição na mesma sequência quando caem num dia
/// que substituiria um plantão (destacadas em amarelo, com legenda dos códigos no fim da planilha).
/// </summary>
public class EscalaCsvService(IEscalaService escalaService, ApplicationDbContext db) : IEscalaCsvService
{
    private const decimal ValorPorUnidadeAuxilio = 20m;
    private const int UnidadesPorPlantao24h = 3;
    private const int UnidadesPorPlantao12h = 2;

    private static readonly string[] CodigosAfastamento = ["FR", "LM", "LO", "LP"];

    private static readonly (string Nome, string Descricao)[] LegendaAfastamento =
    [
        ("FR", "Férias"),
        ("LM", "Licença Médica"),
        ("LP", "Licença Prêmio"),
        ("LO", "Licença Outros"),
    ];

    /// <summary>Um par de cor por grupo (núcleo, ou setor quando não tem núcleo) — tom escuro pra
    /// célula "de cima" na hierarquia (núcleo, ou setor sem núcleo), tom claro da mesma família
    /// pra célula "filha" (setor de um núcleo) — mantém as duas cores visualmente próximas.</summary>
    private static readonly (string Escuro, string Claro)[] PaletaCores =
    [
        ("#1B5E20", "#C8E6C9"), // verde
        ("#0D47A1", "#BBDEFB"), // azul
        ("#4A148C", "#E1BEE7"), // roxo
        ("#E65100", "#FFE0B2"), // laranja
        ("#004D40", "#B2DFDB"), // teal
        ("#880E4F", "#F8BBD0"), // rosa
        ("#1A237E", "#C5CAE9"), // índigo
        ("#3E2723", "#D7CCC8"), // marrom
        ("#263238", "#CFD8DC"), // azul acinzentado
    ];

    private const string CorAmareloAfastamento = "#FFF59D";
    private const string CorNeutraSemLotacao = "#EEEEEE";

    public async Task<Result<(byte[] Content, string FileName)>> GenerateResumidoAsync(
        Guid id,
        string actorLogin,
        CancellationToken cancellationToken = default)
    {
        var detail = await escalaService.GetByIdAsync(id, actorLogin, cancellationToken);
        if (!detail.Succeeded)
        {
            return Result<(byte[], string)>.Failure(detail.Error!);
        }

        var escala = detail.Value!;
        var servidorIds = escala.Servidores.Select(s => s.ServidorId).ToList();
        var lotacoes = await db.Servidores
            .Where(x => servidorIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                SetorSigla = x.Setor != null ? x.Setor.Sigla : null,
                NucleoSigla = x.Setor != null
                    ? (x.Setor.Nucleo != null ? x.Setor.Nucleo.Sigla : null)
                    : (x.Nucleo != null ? x.Nucleo.Sigla : null),
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var linhas = escala.Servidores
            .Select(s =>
            {
                var lotacao = lotacoes.GetValueOrDefault(s.ServidorId);
                return BuildLinha(s, lotacao?.NucleoSigla, lotacao?.SetorSigla);
            })
            .OrderBy(l => l.Servidor.CargoNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Servidor.ServidorNome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var max24h = linhas.Count == 0 ? 0 : linhas.Max(l => l.Dias24h.Count);
        var max12h = linhas.Count == 0 ? 0 : linhas.Max(l => l.Dias12h.Count);

        var bytes = BuildWorkbook(linhas, max24h, max12h);
        var sigla = escala.SetorSigla ?? escala.NucleoSigla ?? "escala";
        var fileName = $"escala-{sigla}-{escala.Ano}-{escala.Mes:00}-resumido.xlsx".Replace(" ", "-");

        return Result<(byte[], string)>.Success((bytes, fileName));
    }

    private static byte[] BuildWorkbook(List<ResumidoLinha> linhas, int max24h, int max12h)
    {
        const int primeiraColuna24h = 8; // A..G = CARGO..VALOR
        var primeiraColuna12h = primeiraColuna24h + max24h;
        var totalColunas = primeiraColuna12h + max12h - 1;

        var corPorGrupo = AtribuirCores(linhas);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Escala");

        // Linha 1: cabeçalho de grupo mesclado "24h"/"12h" — evita repetir "24h"/"12h" em cada
        // coluna de dia, como pedido.
        if (max24h > 0)
        {
            var range = ws.Range(1, primeiraColuna24h, 1, primeiraColuna24h + max24h - 1);
            range.Merge();
            range.FirstCell().Value = "24h";
            EstilizarGrupoHeader(range.FirstCell());
        }

        if (max12h > 0)
        {
            var range = ws.Range(1, primeiraColuna12h, 1, primeiraColuna12h + max12h - 1);
            range.Merge();
            range.FirstCell().Value = "12h";
            EstilizarGrupoHeader(range.FirstCell());
        }

        // Linha 2: cabeçalho de coluna — dias viram só o índice dentro do grupo (1, 2, 3...),
        // já que o "24h"/"12h" já está na linha de cima.
        var cabecalhos = new List<string> { "CARGO", "MATRÍCULA", "NOME", "NÚCLEO", "SETOR", "AUXÍLIO", "VALOR" };
        cabecalhos.AddRange(Enumerable.Range(1, max24h).Select(i => i.ToString()));
        cabecalhos.AddRange(Enumerable.Range(1, max12h).Select(i => i.ToString()));
        for (var c = 0; c < cabecalhos.Count; c++)
        {
            var cell = ws.Cell(2, c + 1);
            cell.Value = cabecalhos[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var linha = 3;
        foreach (var l in linhas)
        {
            ws.Cell(linha, 1).Value = l.Servidor.CargoNome;
            ws.Cell(linha, 2).Value = l.Servidor.Matricula;
            ws.Cell(linha, 3).Value = l.Servidor.ServidorNome;

            EscreverCelulaNucleo(ws.Cell(linha, 4), l.NucleoSigla, corPorGrupo);
            EscreverCelulaSetor(ws.Cell(linha, 5), l.NucleoSigla, l.SetorSigla, corPorGrupo);

            ws.Cell(linha, 6).Value = l.UnidadesAuxilio;
            var celulaValor = ws.Cell(linha, 7);
            celulaValor.Value = l.Valor;
            celulaValor.Style.NumberFormat.Format = "\"R$\" #,##0.00";

            EscreverDias(ws, linha, primeiraColuna24h, l.Dias24h);
            EscreverDias(ws, linha, primeiraColuna12h, l.Dias12h);

            linha++;
        }

        if (linhas.Count > 0)
        {
            ws.Range(2, 1, linha - 1, totalColunas).SetAutoFilter();
        }

        // Legenda dos códigos de afastamento, depois da tabela.
        linha += 1;
        var celulaAmostra = ws.Cell(linha, 1);
        celulaAmostra.Value = "";
        celulaAmostra.Style.Fill.BackgroundColor = XLColor.FromHtml(CorAmareloAfastamento);
        ws.Cell(linha, 2).Value = "= dia de afastamento (em vez de plantão)";
        ws.Cell(linha, 2).Style.Font.Italic = true;
        linha++;

        foreach (var (codigo, descricao) in LegendaAfastamento)
        {
            var celulaCodigo = ws.Cell(linha, 1);
            celulaCodigo.Value = codigo;
            celulaCodigo.Style.Font.Bold = true;
            celulaCodigo.Style.Fill.BackgroundColor = XLColor.FromHtml(CorAmareloAfastamento);
            ws.Cell(linha, 2).Value = descricao;
            linha++;
        }

        ws.SheetView.FreezeRows(2);
        ws.Columns(1, totalColunas).AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static void EstilizarGrupoHeader(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
    }

    /// <summary>Célula NÚCLEO: só pinta quando o servidor tem núcleo (direto ou via setor) — cor
    /// escura do grupo, texto branco em negrito, só a sigla. Sem núcleo, mostra "—" neutro.</summary>
    private static void EscreverCelulaNucleo(
        IXLCell cell,
        string? nucleoSigla,
        IReadOnlyDictionary<string, (string Escuro, string Claro)> corPorGrupo)
    {
        if (nucleoSigla is null)
        {
            cell.Value = "—";
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(CorNeutraSemLotacao);
            return;
        }

        cell.Value = nucleoSigla;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(corPorGrupo[nucleoSigla].Escuro);
    }

    /// <summary>Célula SETOR: tom claro do grupo do núcleo (setor "filho" de um núcleo), ou tom
    /// escuro quando o setor não pertence a nenhum núcleo (é o próprio topo do grupo). Servidor
    /// lotado direto no núcleo (sem setor, "Agentes") mostra "—" neutro.</summary>
    private static void EscreverCelulaSetor(
        IXLCell cell,
        string? nucleoSigla,
        string? setorSigla,
        IReadOnlyDictionary<string, (string Escuro, string Claro)> corPorGrupo)
    {
        if (setorSigla is null)
        {
            cell.Value = "—";
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(CorNeutraSemLotacao);
            return;
        }

        cell.Value = setorSigla;
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (nucleoSigla is not null)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(corPorGrupo[nucleoSigla].Claro);
            cell.Style.Font.FontColor = XLColor.FromHtml("#212121");
        }
        else
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(corPorGrupo[setorSigla].Escuro);
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    /// <summary>Paleta determinística: agrupa por núcleo (ou pelo próprio setor, quando o setor não
    /// tem núcleo) e distribui as cores da paleta em ordem alfabética das siglas encontradas nesta
    /// exportação — não existe catálogo fixo de núcleos/setores pra hardcodar (CRUD livre pelas
    /// telas de admin), então a cor de cada grupo pode mudar entre exportações se a composição de
    /// núcleos/setores envolvidos mudar, mas é estável enquanto ela não mudar.</summary>
    private static Dictionary<string, (string Escuro, string Claro)> AtribuirCores(List<ResumidoLinha> linhas)
    {
        var grupos = linhas
            .Select(l => l.NucleoSigla ?? l.SetorSigla)
            .Where(k => k is not null)
            .Select(k => k!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return grupos
            .Select((k, i) => (k, cor: PaletaCores[i % PaletaCores.Length]))
            .ToDictionary(x => x.k, x => x.cor, StringComparer.OrdinalIgnoreCase);
    }

    private static void EscreverDias(IXLWorksheet ws, int linha, int primeiraColuna, IReadOnlyList<string> valores)
    {
        for (var i = 0; i < valores.Count; i++)
        {
            var cell = ws.Cell(linha, primeiraColuna + i);
            var valor = valores[i];
            if (int.TryParse(valor, out var dia))
            {
                cell.Value = dia;
            }
            else
            {
                cell.Value = valor;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(CorAmareloAfastamento);
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
    }

    private static ResumidoLinha BuildLinha(EscalaServidorDto s, string? nucleoSigla, string? setorSigla)
    {
        var ocorrencias = s.Ocorrencias.OrderBy(o => o.Data).ToList();

        var qtd24h = ocorrencias.Count(o => IsCodigo(o.TipoOcorrenciaCodigo, "PT"));
        var qtd12h = ocorrencias.Count(o => IsCodigo(o.TipoOcorrenciaCodigo, "PD") || IsCodigo(o.TipoOcorrenciaCodigo, "PN"));
        var unidades = qtd24h * UnidadesPorPlantao24h + qtd12h * UnidadesPorPlantao12h;

        var temAfastamento = ocorrencias.Any(o => CodigosAfastamento.Any(c => IsCodigo(o.TipoOcorrenciaCodigo, c)));

        // A "família" do servidor (24h ou 12h) decide em qual sequência de colunas as ausências
        // (férias/licença) dele entram — quem tem qualquer plantão de 24h no mês é 24h; senão,
        // quem tem qualquer plantão de 12h é 12h; sem nenhum dos dois mas com afastamento no mês
        // (ex.: licença o mês inteiro, sem plantão algum), cai em 24h por padrão — só pra não
        // desaparecer da planilha sem nenhuma marcação; de fato sem plantão nem afastamento (ex.:
        // expediente administrativo) não entra em nenhuma das duas sequências.
        var familia = qtd24h > 0 ? Familia.Vinte24h
            : qtd12h > 0 ? Familia.Doze12h
            : temAfastamento ? Familia.Vinte24h
            : Familia.Nenhuma;

        var dias24h = new List<string>();
        var dias12h = new List<string>();
        foreach (var o in ocorrencias)
        {
            if (IsCodigo(o.TipoOcorrenciaCodigo, "PT"))
            {
                dias24h.Add(o.Data.Day.ToString(System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }

            if (IsCodigo(o.TipoOcorrenciaCodigo, "PD") || IsCodigo(o.TipoOcorrenciaCodigo, "PN"))
            {
                dias12h.Add(o.Data.Day.ToString(System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }

            if (CodigosAfastamento.Any(c => IsCodigo(o.TipoOcorrenciaCodigo, c)))
            {
                if (familia == Familia.Vinte24h)
                {
                    dias24h.Add(o.TipoOcorrenciaCodigo.ToUpperInvariant());
                }
                else if (familia == Familia.Doze12h)
                {
                    dias12h.Add(o.TipoOcorrenciaCodigo.ToUpperInvariant());
                }
            }
        }

        return new ResumidoLinha(s, unidades, unidades * ValorPorUnidadeAuxilio, dias24h, dias12h, nucleoSigla, setorSigla);
    }

    private static bool IsCodigo(string codigo, string alvo) =>
        string.Equals(codigo, alvo, StringComparison.OrdinalIgnoreCase);

    private enum Familia
    {
        Nenhuma,
        Vinte24h,
        Doze12h,
    }

    private sealed record ResumidoLinha(
        EscalaServidorDto Servidor,
        int UnidadesAuxilio,
        decimal Valor,
        List<string> Dias24h,
        List<string> Dias12h,
        string? NucleoSigla,
        string? SetorSigla);
}
