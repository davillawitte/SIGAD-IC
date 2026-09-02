using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// A numeração de equipe ("Equipe 01", "Equipe 02", ...) é sempre por setor, nunca uma
/// sequência global da escala resumida inteira — dois setores diferentes podem cada um ter
/// sua própria "Equipe 01". Reproduz o bug relatado: Química com duas equipes e STF com uma
/// (a de STF nascendo "Equipe 03" em vez de "Equipe 01" porque a numeração vinha de uma
/// contagem que somava as equipes dos dois setores). `ConfigurarEquipeAsync` agora deriva
/// nome/ordem no servidor a partir de `setor.Equipes.Count`, então não depende mais de o
/// cliente calcular certo (nem de qual aba/setor estava ativo no momento do clique).
/// </summary>
public class EscalaResumidaEquipeNumeracaoPorSetorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.numeracao";

    private async Task<(Guid QuimicaSetorId, Guid StfSetorId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Numeração");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Numeração", "NNU", chefe.Id);
            var quimica = b.AdicionarSetor("Química Forense", "QUI", nucleo);
            var stf = b.AdicionarSetor("Toxicologia Forense", "STF", nucleo);

            return (quimica.Id, stf.Id);
        });

    [Fact]
    public async Task Cada_setor_numera_suas_equipes_a_partir_de_01_independente_dos_outros_setores()
    {
        var (quimicaSetorId, stfSetorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var nucleoId = (await db.Setores.FindAsync(quimicaSetorId))!.NucleoId!.Value;
        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 3, null), ChefeNucleo)).Value!;

        var comSetores = (await service.ConfigurarSetoresAsync(
            criada.Id,
            new ConfigurarSetoresRequest([
                new ConfigurarSetorItem(quimicaSetorId, 1),
                new ConfigurarSetorItem(stfSetorId, 2),
            ]),
            ChefeNucleo)).Value!;
        var quimicaResumidaSetorId = comSetores.Setores.Single(s => s.SetorId == quimicaSetorId).Id;
        var stfResumidaSetorId = comSetores.Setores.Single(s => s.SetorId == stfSetorId).Id;

        // Química recebe duas equipes primeiro.
        await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(quimicaResumidaSetorId), ChefeNucleo);
        var comQuimica2 = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(quimicaResumidaSetorId), ChefeNucleo)).Value!;

        // Só depois o STF recebe a primeira dele — não pode nascer "Equipe 03".
        var comStf = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(stfResumidaSetorId), ChefeNucleo)).Value!;

        var equipeStf = comStf.Setores.Single(s => s.SetorId == stfSetorId).Equipes.Single();
        equipeStf.Nome.ShouldBe("Equipe 01");
        equipeStf.Ordem.ShouldBe(1);

        var equipesQuimica = comQuimica2.Setores.Single(s => s.SetorId == quimicaSetorId)
            .Equipes.OrderBy(e => e.Ordem).ToList();
        equipesQuimica.Count.ShouldBe(2);
        equipesQuimica[0].Nome.ShouldBe("Equipe 01");
        equipesQuimica[1].Nome.ShouldBe("Equipe 02");

        // Remover a "Equipe 02" da Química não pode mexer na "Equipe 01" do STF.
        var equipeQuimica2Id = equipesQuimica[1].Id;
        var final = (await service.RemoverEquipeAsync(criada.Id, equipeQuimica2Id, ChefeNucleo)).Value!;

        var equipeStfFinal = final.Setores.Single(s => s.SetorId == stfSetorId).Equipes.Single();
        equipeStfFinal.Nome.ShouldBe("Equipe 01");
        equipeStfFinal.Ordem.ShouldBe(1);

        var equipeQuimicaFinal = final.Setores.Single(s => s.SetorId == quimicaSetorId).Equipes.Single();
        equipeQuimicaFinal.Nome.ShouldBe("Equipe 01");
        equipeQuimicaFinal.Ordem.ShouldBe(1);
    }
}
