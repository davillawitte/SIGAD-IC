using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Grupo "Agentes" na escala resumida: servidores lotados direto no núcleo (à disposição,
/// sem setor específico) entram como uma coluna própria na grade, ao lado dos setores reais —
/// representado por um <c>EscalaResumidaSetor</c> com <c>SetorId</c> nulo.
/// </summary>
public class EscalaResumidaAgentesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 5;
    private const string ChefeNucleo = "chefe.nucleo.agentes";

    private async Task<(Guid NucleoId, Guid SetorId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Teste", "NTS", chefe.Id);
            var setor = b.AdicionarSetor("Setor do Núcleo", "SDN", nucleo);
            b.AdicionarServidorNoNucleo(nucleo, "Agente Solto");

            return (nucleo.Id, setor.Id);
        });

    [Fact]
    public async Task Configurar_setores_aceita_um_grupo_de_agentes_ao_lado_do_setor()
    {
        var (nucleoId, setorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), ChefeNucleo);
        criada.Succeeded.ShouldBeTrue(criada.Error);

        var configurado = await service.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest(
            [
                new ConfigurarSetorItem(setorId, 1),
                new ConfigurarSetorItem(null, 2),
            ]),
            ChefeNucleo);

        configurado.Succeeded.ShouldBeTrue(configurado.Error);
        var grupos = configurado.Value!.Setores.OrderBy(s => s.Ordem).ToList();
        grupos.Count.ShouldBe(2);
        grupos[0].SetorId.ShouldBe(setorId);
        grupos[1].SetorId.ShouldBeNull();
        grupos[1].SetorNome.ShouldBe("Agentes");
        grupos[1].SetorSigla.ShouldBe("Agentes");
    }

    [Fact]
    public async Task Configurar_setores_rejeita_mais_de_um_grupo_de_agentes()
    {
        var (nucleoId, _) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), ChefeNucleo);
        criada.Succeeded.ShouldBeTrue(criada.Error);

        var resultado = await service.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest(
            [
                new ConfigurarSetorItem(null, 1),
                new ConfigurarSetorItem(null, 2),
            ]),
            ChefeNucleo);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Só pode haver um grupo de Agentes.");
    }

    [Fact]
    public async Task Remover_grupo_de_agentes_reenviando_sem_ele()
    {
        var (nucleoId, setorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), ChefeNucleo);
        criada.Succeeded.ShouldBeTrue(criada.Error);

        await service.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest(
            [
                new ConfigurarSetorItem(setorId, 1),
                new ConfigurarSetorItem(null, 2),
            ]),
            ChefeNucleo);

        var semAgentes = await service.ConfigurarSetoresAsync(
            criada.Value!.Id,
            new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]),
            ChefeNucleo);

        semAgentes.Succeeded.ShouldBeTrue(semAgentes.Error);
        semAgentes.Value!.Setores.ShouldHaveSingleItem();
        semAgentes.Value!.Setores[0].SetorId.ShouldBe(setorId);
    }

    [Fact]
    public async Task Posicao_do_rodizio_com_segunda_pessoa_gera_dias_com_os_dois_nomes_combinados()
    {
        Guid nucleoId = default, agente1Id = default, agente2Id = default;
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo 2P");
            b.AdicionarUsuario(chefe, "chefe.nucleo.2pessoas", CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Reforço", "NRF", chefe.Id);
            var a1 = b.AdicionarServidorNoNucleo(nucleo, "Agente Principal");
            var a2 = b.AdicionarServidorNoNucleo(nucleo, "Agente Reforço");

            nucleoId = nucleo.Id;
            agente1Id = a1.Id;
            agente2Id = a2.Id;
            return true;
        });

        const string chefeLogin = "chefe.nucleo.2pessoas";
        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), chefeLogin)).Value!;
        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(null, 1)]), chefeLogin)).Value!;
        var agentesSetorId = comSetor.Setores[0].Id;
        var comEquipe = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(agentesSetorId), chefeLogin)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes[0].Id;

        var ancora = new DateOnly(Ano, Mes, 1);
        var final = (await service.ConfigurarRotacaoAsync(
            criada.Id, equipeId,
            new ConfigurarRotacaoRequest(ancora, [new RotacaoMembroItem(0, agente1Id, agente2Id)]),
            chefeLogin)).Value!;

        var equipe = final.Setores[0].Equipes[0];
        equipe.Rotacao.ShouldHaveSingleItem();
        equipe.Rotacao[0].ServidorId2.ShouldBe(agente2Id);
        equipe.Rotacao[0].ServidorNome2.ShouldBe("Agente Reforço");

        var dia1 = equipe.Dias.Single(d => d.Data == ancora);
        dia1.ServidorNome.ShouldBe("Agente Principal");
        dia1.ServidorId2.ShouldBe(agente2Id);
        dia1.ServidorNome2.ShouldBe("Agente Reforço");
        dia1.IsFolga2.ShouldBeFalse();
        dia1.Rotulo.ShouldBe("Agente Principal + Agente Reforço");
    }

    [Fact]
    public async Task Override_manual_de_um_dia_aceita_segunda_pessoa_marcada_como_DO()
    {
        Guid nucleoId = default, agenteId = default;
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo DO2");
            b.AdicionarUsuario(chefe, "chefe.nucleo.do2", CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo DO2", "NDO", chefe.Id);
            var a1 = b.AdicionarServidorNoNucleo(nucleo, "Agente Único");

            nucleoId = nucleo.Id;
            agenteId = a1.Id;
            return true;
        });

        const string chefeLogin = "chefe.nucleo.do2";
        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, Ano, Mes, null), chefeLogin)).Value!;
        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(null, 1)]), chefeLogin)).Value!;
        var comEquipe = (await service.ConfigurarEquipeAsync(
            criada.Id, new ConfigurarEquipeRequest(comSetor.Setores[0].Id), chefeLogin)).Value!;
        var equipeId = comEquipe.Setores[0].Equipes[0].Id;

        var data = new DateOnly(Ano, Mes, 5);
        var resultado = await service.UpsertDiaAsync(
            criada.Id, equipeId,
            new UpsertDiaRequest(data, agenteId, TextoLivre: null, IsFolga: false, ServidorId2: null, IsFolga2: true),
            chefeLogin);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        var dia = resultado.Value!.Setores[0].Equipes[0].Dias.Single(d => d.Data == data);
        dia.ServidorId2.ShouldBeNull();
        dia.IsFolga2.ShouldBeTrue();
        dia.Rotulo.ShouldBe("Agente Único + DO");
    }
}
