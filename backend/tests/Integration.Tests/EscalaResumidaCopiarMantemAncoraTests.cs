using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Copiar uma escala resumida pro mês seguinte deve só continuar a sequência do rodízio, sem
/// reancorar a partir de nenhum dia específico do mês de origem — o rodízio é calculado por
/// contagem de dias corrida (<c>EscalaResumidaRotacaoExpander.ExpandSetor</c>), então manter a
/// mesma âncora entre os meses já preserva a fase automaticamente.
/// </summary>
public class EscalaResumidaCopiarMantemAncoraTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.copia";

    [Fact]
    public async Task Copiar_para_o_mes_seguinte_mantem_a_ancora_e_a_fase_do_rodizio()
    {
        var (nucleoId, setorId, servidorIds) = await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Cópia");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Cópia", "NCP", chefe.Id);
            var setor = b.AdicionarSetor("Setor Cópia", "SCP", nucleo);
            var a = b.AdicionarServidor(setor, "Servidor A Posição Zero");
            var bb = b.AdicionarServidor(setor, "Servidor B Posição Um");
            var c = b.AdicionarServidor(setor, "Servidor C Posição Dois");
            var d = b.AdicionarServidor(setor, "Servidor D Posição Três");

            return (nucleo.Id, setor.Id, new[] { a.Id, bb.Id, c.Id, d.Id });
        });

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var origem = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 9, null), ChefeNucleo)).Value!;
        var comSetor = (await service.ConfigurarSetoresAsync(
            origem.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;
        var comEquipe = (await service.ConfigurarEquipeAsync(
            origem.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes.Single().Id;

        // Pool de 4 (não divide igualmente os 30 dias de setembro) — se a cópia reancorasse no
        // dia 1 de outubro em vez de manter a âncora de setembro, a posição do dia 1 de outubro
        // mudaria (0 em vez de 2), o que o teste pega.
        var ancora = new DateOnly(2026, 9, 1);
        var configurada = await service.ConfigurarRotacaoAsync(
            origem.Id,
            equipeId,
            new ConfigurarRotacaoRequest(
                ancora,
                [
                    new RotacaoMembroItem(0, servidorIds[0]),
                    new RotacaoMembroItem(1, servidorIds[1]),
                    new RotacaoMembroItem(2, servidorIds[2]),
                    new RotacaoMembroItem(3, servidorIds[3]),
                ]),
            ChefeNucleo);
        configurada.Succeeded.ShouldBeTrue(configurada.Error);

        var copiada = await service.CopiarAsync(
            origem.Id, new CopiarEscalaResumidaRequest(2026, 10), ChefeNucleo);

        copiada.Succeeded.ShouldBeTrue(copiada.Error);
        var equipeCopiada = copiada.Value!.Setores[0].Equipes.Single();

        equipeCopiada.DataInicioCiclo.ShouldBe(ancora, "a âncora deve ser mantida, não recalculada a partir do novo mês");

        var dia1Outubro = equipeCopiada.Dias.Single(d => d.Data == new DateOnly(2026, 10, 1));
        // 1º/out é 30 dias depois da âncora (setembro tem 30 dias); 30 % 4 == 2 → posição 2 (Servidor C).
        dia1Outubro.ServidorId.ShouldBe(
            servidorIds[2],
            "mantendo a âncora de setembro, o dia 1 de outubro continua a mesma sequência (posição 2 do pool de 4) — não reinicia do zero");
    }
}
