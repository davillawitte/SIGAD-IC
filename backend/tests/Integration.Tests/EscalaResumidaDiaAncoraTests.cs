using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Reproduz, pelo fluxo real (`ConfigurarRotacaoAsync` → `RegerarSetorAsync` →
/// `GetByIdAsync`), o relato do usuário: com a âncora do rodízio no dia 1 do mês, o próprio
/// dia 1 aparecia em branco na grade, mesmo com a posição 0 preenchida — mas o dia 7 (mesma
/// posição 0, um ciclo depois, num pool de 2) aparecia certo. O teste unitário do expander
/// (`EscalaResumidaRotacaoExpanderTests`) já cobria a fórmula isolada e passava; este cobre o
/// caminho completo que a tela usa, que o outro não exercita.
/// </summary>
public class EscalaResumidaDiaAncoraTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.ancora";

    private async Task<(Guid NucleoId, Guid SetorId, Guid ServidorAId, Guid ServidorBId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Âncora");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Âncora", "NAN", chefe.Id);
            var setor = b.AdicionarSetor("Setor Âncora", "SAN", nucleo);
            var servidorA = b.AdicionarServidor(setor, "Servidor A Posição Zero");
            var servidorB = b.AdicionarServidor(setor, "Servidor B Posição Um");

            return (nucleo.Id, setor.Id, servidorA.Id, servidorB.Id);
        });

    [Fact]
    public async Task Dia_da_ancora_mostra_a_posicao_0_do_pool_igual_aos_ciclos_seguintes()
    {
        var (nucleoId, setorId, servidorAId, servidorBId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 9, null), ChefeNucleo)).Value!;

        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;

        var comEquipe = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes.Single().Id;

        var ancora = new DateOnly(2026, 9, 1);
        ancora.ShouldBe(criada.DataInicio, "a âncora do teste precisa ser o próprio dia 1 da escala, igual ao relato");

        var resultado = await service.ConfigurarRotacaoAsync(
            criada.Id,
            equipeId,
            new ConfigurarRotacaoRequest(
                ancora,
                [
                    new RotacaoMembroItem(0, servidorAId),
                    new RotacaoMembroItem(1, servidorBId),
                ]),
            ChefeNucleo);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        var dias = resultado.Value!.Setores[0].Equipes[0].Dias.OrderBy(d => d.Data).ToList();

        var dia1 = dias.Single(d => d.Data == ancora);
        dia1.ServidorId.ShouldBe(servidorAId, "dia 1 é a própria âncora — deveria ser a posição 0, não ficar em branco");
        dia1.Rotulo.ShouldNotBeNullOrWhiteSpace();

        var dia2 = dias.Single(d => d.Data == ancora.AddDays(1));
        dia2.ServidorId.ShouldBe(servidorBId);

        var dia7 = dias.Single(d => d.Data == ancora.AddDays(6));
        dia7.ServidorId.ShouldBe(servidorAId, "dia 7 é a posição 0 de novo (pool de 2, 6 dias depois) — igual ao dia 1");
    }

    /// <summary>
    /// Hipótese pra explicar o print do usuário (posição 0 certa na configuração, dia 7 certo
    /// na grade, só o dia 1 em branco): algum clique anterior deixou uma célula "Manual" em
    /// branco pro dia 1 — e `RegerarSetorAsync` propositalmente nunca sobrescreve uma célula
    /// `Manual` (é assim que preserva edição manual do usuário). Sem nenhum jeito na UI de
    /// reverter uma célula manual pra regra (`reverterDia` existe no serviço/API mas não é
    /// chamado de lugar nenhum no frontend hoje), a célula fica presa em branco pra sempre,
    /// mesmo com o rodízio certinho.
    /// </summary>
    [Fact]
    public async Task Override_manual_em_branco_no_dia_1_sobrevive_a_regeneracao_do_rodizio()
    {
        var (nucleoId, setorId, servidorAId, servidorBId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 9, null), ChefeNucleo)).Value!;
        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;
        var comEquipe = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes.Single().Id;
        var ancora = new DateOnly(2026, 9, 1);

        // Simula um clique acidental que grava uma célula manual em branco pro dia 1 ANTES
        // (ou depois, tanto faz pra este teste) de configurar o rodízio.
        await service.UpsertDiaAsync(
            criada.Id, equipeId, new UpsertDiaRequest(ancora, null, null, false), ChefeNucleo);

        var resultado = await service.ConfigurarRotacaoAsync(
            criada.Id,
            equipeId,
            new ConfigurarRotacaoRequest(
                ancora,
                [
                    new RotacaoMembroItem(0, servidorAId),
                    new RotacaoMembroItem(1, servidorBId),
                ]),
            ChefeNucleo);

        var dias = resultado.Value!.Setores[0].Equipes[0].Dias.OrderBy(d => d.Data).ToList();
        var dia1 = dias.Single(d => d.Data == ancora);

        dia1.Origem.ShouldBe(OrigemOcorrencia.Manual);
        dia1.ServidorId.ShouldBeNull("reproduz o sintoma: célula manual em branco sobrevive à regeneração do rodízio");
    }
}
