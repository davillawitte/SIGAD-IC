using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Ao remover uma equipe, as irmãs restantes fecham o buraco na numeração — "Equipe 02" e
/// "Equipe 03" viram "Equipe 01" e "Equipe 02" quando a antiga "Equipe 01" é removida. Só
/// renomeia quem ainda tem o nome auto-gerado padrão; nome customizado fica intacto.
/// </summary>
public class EscalaResumidaEquipeRemocaoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.remocao";

    private async Task<(Guid EscalaResumidaId, Guid SetorId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Remoção", "NRM", chefe.Id);
            var setor = b.AdicionarSetor("Setor Remoção", "SRM", nucleo);

            return (Guid.Empty, setor.Id);
        });

    [Fact]
    public async Task Remover_a_primeira_equipe_renumera_as_demais_fechando_o_buraco()
    {
        var (_, setorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(
                (await db.Setores.FindAsync(setorId))!.NucleoId!.Value, 2026, 6, null),
            ChefeNucleo)).Value!;

        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;

        Guid IdDaEquipe(EscalaResumidaDetailDto d, string nome) =>
            d.Setores[0].Equipes.Single(e => e.Nome == nome).Id;

        var d1 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipe1Id = IdDaEquipe(d1, "Equipe 01");

        var d2 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;

        var d3 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;

        var final = (await service.RemoverEquipeAsync(criada.Id, equipe1Id, ChefeNucleo)).Value!;

        var equipes = final.Setores[0].Equipes.OrderBy(e => e.Ordem).ToList();
        equipes.Count.ShouldBe(2);
        equipes[0].Nome.ShouldBe("Equipe 01");
        equipes[0].Ordem.ShouldBe(1);
        equipes[1].Nome.ShouldBe("Equipe 02");
        equipes[1].Ordem.ShouldBe(2);
    }

    [Fact]
    public async Task Remover_equipe_preserva_nome_customizado_das_irmas()
    {
        var (_, setorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(
                (await db.Setores.FindAsync(setorId))!.NucleoId!.Value, 2026, 6, null),
            ChefeNucleo)).Value!;

        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;

        var d1 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipe1Id = d1.Setores[0].Equipes.Single(e => e.Nome == "Equipe 01").Id;

        var d2 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), ChefeNucleo)).Value!;
        var equipe2Id = d2.Setores[0].Equipes.Single(e => e.Nome == "Equipe 02").Id;

        await service.AtualizarEquipeAsync(
            criada.Id, equipe2Id, new AtualizarEquipeRequest("Plantão Alfa", 2), ChefeNucleo);

        var final = (await service.RemoverEquipeAsync(criada.Id, equipe1Id, ChefeNucleo)).Value!;

        var equipe = final.Setores[0].Equipes.Single();
        equipe.Nome.ShouldBe("Plantão Alfa");
        equipe.Ordem.ShouldBe(1);
    }
}
