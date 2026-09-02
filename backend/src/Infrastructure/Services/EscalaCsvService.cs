using System.Globalization;
using System.Text;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;

namespace TemplateSistema.Infrastructure.Services;

/// <summary>
/// Gera o CSV "resumido" (auxílio-alimentação) de uma escala: por servidor, cargo/matrícula/
/// núcleo/setor, quantidade e valor do auxílio (cada plantão de 24h vale 3 unidades, cada
/// plantão de 12h vale 2 — R$ 20,00 cada), e a sequência cronológica dos dias de plantão do mês,
/// com férias/licenças (FR/LM/LO/LP) ocupando uma posição na mesma sequência quando caem num dia
/// que substituiria um plantão.
/// </summary>
public class EscalaCsvService(IEscalaService escalaService) : IEscalaCsvService
{
    private const decimal ValorPorUnidadeAuxilio = 20m;
    private const int UnidadesPorPlantao24h = 3;
    private const int UnidadesPorPlantao12h = 2;

    private static readonly string[] CodigosAfastamento = ["FR", "LM", "LO", "LP"];

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
        var linhas = escala.Servidores
            .Select(s => BuildLinha(s, escala.NucleoSigla, escala.SetorSigla))
            .OrderBy(l => l.Servidor.CargoNome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Servidor.ServidorNome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var max24h = linhas.Count == 0 ? 0 : linhas.Max(l => l.Dias24h.Count);
        var max12h = linhas.Count == 0 ? 0 : linhas.Max(l => l.Dias12h.Count);

        var csv = new StringBuilder();
        csv.AppendLine(BuildHeader(max24h, max12h));
        foreach (var linha in linhas)
        {
            csv.AppendLine(BuildLinhaCsv(linha, max24h, max12h));
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
        var sigla = escala.SetorSigla ?? escala.NucleoSigla ?? "escala";
        var fileName = $"escala-{sigla}-{escala.Ano}-{escala.Mes:00}-resumido.csv".Replace(" ", "-");

        return Result<(byte[], string)>.Success((bytes, fileName));
    }

    private static string BuildHeader(int max24h, int max12h)
    {
        var cols = new List<string> { "CARGO", "MATRÍCULA", "NOME", "NÚCLEO", "SETOR", "AUXÍLIO", "VALOR" };
        cols.AddRange(Enumerable.Range(1, max24h).Select(i => $"24H_{i}"));
        cols.AddRange(Enumerable.Range(1, max12h).Select(i => $"12H_{i}"));
        return string.Join(';', cols.Select(Escape));
    }

    private static string BuildLinhaCsv(ResumidoLinha linha, int max24h, int max12h)
    {
        var s = linha.Servidor;
        var cols = new List<string>
        {
            s.CargoNome,
            s.Matricula,
            s.ServidorNome,
            linha.NucleoSigla ?? "—",
            linha.SetorSigla ?? "—",
            linha.UnidadesAuxilio.ToString(CultureInfo.InvariantCulture),
            $"R$ {FormatValorBr(linha.Valor)}",
        };
        cols.AddRange(PadRight(linha.Dias24h, max24h));
        cols.AddRange(PadRight(linha.Dias12h, max12h));
        return string.Join(';', cols.Select(Escape));
    }

    private static IEnumerable<string> PadRight(IReadOnlyList<string> valores, int total)
    {
        for (var i = 0; i < total; i++)
        {
            yield return i < valores.Count ? valores[i] : string.Empty;
        }
    }

    private static ResumidoLinha BuildLinha(EscalaServidorDto s, string? nucleoSigla, string? setorSigla)
    {
        var ocorrencias = s.Ocorrencias.OrderBy(o => o.Data).ToList();

        var qtd24h = ocorrencias.Count(o => IsCodigo(o.TipoOcorrenciaCodigo, "PT"));
        var qtd12h = ocorrencias.Count(o => IsCodigo(o.TipoOcorrenciaCodigo, "PD") || IsCodigo(o.TipoOcorrenciaCodigo, "PN"));
        var unidades = qtd24h * UnidadesPorPlantao24h + qtd12h * UnidadesPorPlantao12h;

        // A "família" do servidor (24h ou 12h) decide em qual sequência de colunas as ausências
        // (férias/licença) dele entram — quem tem qualquer plantão de 24h no mês é 24h; senão,
        // quem tem qualquer plantão de 12h é 12h; sem nenhum dos dois (ex.: expediente
        // administrativo, ou ausente o mês inteiro) não entra em nenhuma das duas sequências.
        var familia = qtd24h > 0 ? Familia.Vinte24h : qtd12h > 0 ? Familia.Doze12h : Familia.Nenhuma;

        var dias24h = new List<string>();
        var dias12h = new List<string>();
        foreach (var o in ocorrencias)
        {
            if (IsCodigo(o.TipoOcorrenciaCodigo, "PT"))
            {
                dias24h.Add(o.Data.Day.ToString(CultureInfo.InvariantCulture));
                continue;
            }

            if (IsCodigo(o.TipoOcorrenciaCodigo, "PD") || IsCodigo(o.TipoOcorrenciaCodigo, "PN"))
            {
                dias12h.Add(o.Data.Day.ToString(CultureInfo.InvariantCulture));
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

    /// <summary>
    /// Formata em "1.234,56" (padrão pt-BR) sem depender de <see cref="CultureInfo"/> nomeada —
    /// a API roda com <c>InvariantGlobalization</c> ligado, então "pt-BR" não existe em runtime.
    /// </summary>
    private static string FormatValorBr(decimal valor)
    {
        var invariante = valor.ToString("F2", CultureInfo.InvariantCulture);
        var partes = invariante.Split('.');
        var inteiro = partes[0];
        var negativo = inteiro.StartsWith('-');
        if (negativo)
        {
            inteiro = inteiro[1..];
        }

        var agrupado = new StringBuilder();
        for (var i = 0; i < inteiro.Length; i++)
        {
            if (i > 0 && (inteiro.Length - i) % 3 == 0)
            {
                agrupado.Append('.');
            }

            agrupado.Append(inteiro[i]);
        }

        return $"{(negativo ? "-" : "")}{agrupado},{partes[1]}";
    }

    private static string Escape(string? valor)
    {
        valor ??= string.Empty;
        return valor.Contains(';') || valor.Contains('"') || valor.Contains('\n')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
    }

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
