using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Fim a fim (com banco) da troca de pool entre equipes irmãs — garante que a fiação
/// (`Include`s, agrupamento por setor em `RegerarSetorAsync`) realmente persiste os dias da
/// equipe 1 corretamente quando é a equipe 2 quem acabou de salvar seu rodízio, e vice-versa
/// (a lógica pura já é coberta por <see cref="EscalaResumidaRotacaoExpanderTests"/>).
/// </summary>
public class EscalaResumidaRotacaoSwapIntegrationTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.swap";

    private async Task<(Guid EscalaResumidaId, Guid SetorId, Guid Servidor1, Guid Servidor2, Guid Servidor3, Guid Servidor4)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Swap", "NSW", chefe.Id);
            var setor = b.AdicionarSetor("Setor Swap", "SSW", nucleo);

            var s1 = b.AdicionarServidor(setor, "Servidor Um");
            var s2 = b.AdicionarServidor(setor, "Servidor Dois");
            var s3 = b.AdicionarServidor(setor, "Servidor Três");
            var s4 = b.AdicionarServidor(setor, "Servidor Quatro");

            return (Guid.Empty, setor.Id, s1.Id, s2.Id, s3.Id, s4.Id);
        });

    [Fact]
    public async Task Salvar_rodizio_da_equipe_2_regera_tambem_os_dias_da_equipe_1_irma()
    {
        var (_, setorId, s1, s2, s3, s4) = await PrepararAsync();
        var ancora = new DateOnly(2026, 9, 1);

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(
                (await db.Setores.FindAsync(setorId))!.NucleoId!.Value, ancora.Year, ancora.Month, null),
            ChefeNucleo)).Value!;

        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id,
            new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]),
            ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;

        var comEquipe1 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipe1Id = comEquipe1.Setores[0].Equipes.Single(e => e.Nome == "Equipe 01").Id;

        var comEquipe2 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipe2Id = comEquipe2.Setores[0].Equipes.Single(e => e.Nome == "Equipe 02").Id;

        // Equipe 01: [s1, s2] (pool de 2). Equipe 02: [s3, s4] (pool de 2, mesma âncora).
        await service.ConfigurarRotacaoAsync(
            criada.Id, equipe1Id,
            new ConfigurarRotacaoRequest(ancora, [new RotacaoMembroItem(0, s1), new RotacaoMembroItem(1, s2)]),
            ChefeNucleo);

        var final = (await service.ConfigurarRotacaoAsync(
            criada.Id, equipe2Id,
            new ConfigurarRotacaoRequest(ancora, [new RotacaoMembroItem(0, s3), new RotacaoMembroItem(1, s4)]),
            ChefeNucleo)).Value!;

        var equipe1 = final.Setores[0].Equipes.Single(e => e.Id == equipe1Id);
        var equipe2 = final.Setores[0].Equipes.Single(e => e.Id == equipe2Id);

        var dia1Eq1 = equipe1.Dias.OrderBy(d => d.Data).First(d => d.Data == ancora);
        var dia3Eq1 = equipe1.Dias.OrderBy(d => d.Data).First(d => d.Data == ancora.AddDays(2));
        var dia1Eq2 = equipe2.Dias.OrderBy(d => d.Data).First(d => d.Data == ancora);
        var dia3Eq2 = equipe2.Dias.OrderBy(d => d.Data).First(d => d.Data == ancora.AddDays(2));

        // Ciclo 1 (dias 1-2): cada equipe com o próprio pool.
        dia1Eq1.ServidorId.ShouldBe(s1);
        dia1Eq2.ServidorId.ShouldBe(s3);

        // Ciclo 2 (a partir do dia 3): pools trocados — equipe 1 mostra quem estava na
        // equipe 2 (e foi salvo DEPOIS dela), confirmando que salvar o rodízio da equipe 2
        // regerou também os dias já existentes da equipe 1.
        dia3Eq1.ServidorId.ShouldBe(s3);
        dia3Eq2.ServidorId.ShouldBe(s1);
    }
}
