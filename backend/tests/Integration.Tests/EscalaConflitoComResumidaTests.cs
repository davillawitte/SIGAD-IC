using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Escala resumida é só uma etapa de planejamento/visualização anterior à escala de fato — ela
/// não gera nem sofre conflito de escalonamento (ver `EscalaConflitoChecker`). Um servidor pode
/// estar no rodízio de uma escala resumida e, ao mesmo tempo, numa escala real no mesmo
/// período, sem que isso seja bloqueado em nenhuma direção.
/// </summary>
public class EscalaConflitoComResumidaTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 1;

    private async Task<(Guid ServidorId, Guid EscalaResumidaId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var nucleo = b.AdicionarNucleo("Núcleo Conflito", "NCF");
            var setor = b.AdicionarSetor("Setor Conflito", "SCF", nucleo);
            var servidor = b.AdicionarServidor(setor, "Servidor Conflitante");

            var escalaResumida = EscalaResumida.Create(nucleo.Id, Ano, Mes, createdBy: "teste");
            b.Db.EscalasResumidas.Add(escalaResumida);

            var escalaResumidaSetor = EscalaResumidaSetor.Create(
                escalaResumida.Id, setor.Id, 1, setor.Nome, setor.Sigla, "teste");
            b.Db.EscalaResumidaSetores.Add(escalaResumidaSetor);

            var equipe = EscalaResumidaEquipe.Create(escalaResumidaSetor.Id, "Equipe 01", 1, "teste");
            b.Db.EscalaResumidaEquipes.Add(equipe);

            var membro = EscalaResumidaRotacaoMembro.Create(equipe.Id, 0, servidor.Id, createdBy: "teste");
            b.Db.EscalaResumidaRotacaoMembros.Add(membro);

            return (servidor.Id, escalaResumida.Id);
        });

    [Fact]
    public async Task Servidor_no_rodizio_da_resumida_nao_conflita_com_escala_do_mesmo_periodo()
    {
        var (servidorId, _) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaService(db);

        var conflitos = await service.CheckConflitosServidoresAsync(
            new CheckConflitosServidoresRequest(Ano, Mes, [servidorId]), "qualquer-login");

        conflitos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Configurar_rotacao_de_resumida_nao_e_bloqueada_por_servidor_ja_numa_escala_real()
    {
        const string chefeNucleo = "chefe.nucleo.conflito2";

        var (setorId, servidorId) = await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Conflito 2");
            b.AdicionarUsuario(chefe, chefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Conflito 2", "NCF2", chefe.Id);
            var setor = b.AdicionarSetor("Setor Conflito 2", "SCF2", nucleo);
            var servidor = b.AdicionarServidor(setor, "Servidor Já Escalado");

            var escala = b.AdicionarEscala(setor, Ano, Mes);
            b.AdicionarEscalaServidor(escala, servidor);

            return (setor.Id, servidor.Id);
        });

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var nucleoId = (await db.Setores.FindAsync(setorId))!.NucleoId!.Value;
        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), chefeNucleo)).Value!;

        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), chefeNucleo)).Value!;
        var escalaResumidaSetorId = comSetor.Setores[0].Id;

        var comEquipe = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(escalaResumidaSetorId), chefeNucleo)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes.Single(e => e.Nome == "Equipe 01").Id;

        var request = new ConfigurarRotacaoRequest(
            new DateOnly(Ano, Mes, 1), [new RotacaoMembroItem(0, servidorId)]);
        var resultado = await service.ConfigurarRotacaoAsync(
            criada.Id, equipeId, request, chefeNucleo, CancellationToken.None);

        resultado.Succeeded.ShouldBeTrue();
    }
}
