using Shouldly;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Calendário do mês (quem trabalha em cada dia) das telas de Gestão do Setor e Gestão
/// Institucional: uma consulta só, com as ocorrências achatadas e já limitadas ao alcance do
/// usuário — o escopo decide se enxerga apenas o que chefia ou todo o Instituto.
/// </summary>
public class EscalaCalendarioMesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 5;
    private const string ChefeSetor = "chefe.setor";
    private const string DirecaoInstitucional = "direcao.institucional";
    private const string SemChefia = "sem.chefia";

    private sealed record Contexto(
        Guid SetorId,
        Guid OutroSetorId,
        Guid DirecaoSetorId,
        Guid ServidorId,
        Guid ColegaId,
        Guid ServidorDeOutroSetorId);

    /// <summary>
    /// Setor NB (com chefe próprio e dois servidores), setor SI alheio e a Direção do IC —
    /// todos com marcação no mesmo mês.
    /// </summary>
    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var diretor = b.AdicionarServidor(direcao, "Diretor do IC");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarSuperAdmin(
                diretor,
                DirecaoInstitucional,
                CatalogSeed.PerfilDirecaoIcId,
                CatalogSeed.PerfilChefeSetorId);

            var nb = b.AdicionarSetor("Núcleo de Balística", "NB");
            var chefe = b.AdicionarServidor(nb, "Chefe do NB");
            b.AdicionarChefia(nb, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, ChefeSetor, CatalogSeed.PerfilChefeSetorId);
            var servidor = b.AdicionarServidor(nb, "Ana Peritos");
            var colega = b.AdicionarServidor(nb, "Bruno Agentes");

            var si = b.AdicionarSetor("Setor Independente", "SI");
            var servidorSi = b.AdicionarServidor(si, "Carla Independente");
            var semChefia = b.AdicionarServidor(si, "Sem Chefia");
            b.AdicionarUsuario(semChefia, SemChefia, CatalogSeed.PerfilServidorId);

            var escalaNb = b.AdicionarEscala(nb, Ano, Mes);
            Marcar(b, b.AdicionarEscalaServidor(escalaNb, servidor), (1, "M"), (2, "FR"), (3, "D"));
            Marcar(b, b.AdicionarEscalaServidor(escalaNb, colega, ordem: 2), (1, "PT"));

            var escalaSi = b.AdicionarEscala(si, Ano, Mes);
            Marcar(b, b.AdicionarEscalaServidor(escalaSi, servidorSi), (1, "T"));

            var escalaDirecao = b.AdicionarEscala(direcao, Ano, Mes);
            Marcar(b, b.AdicionarEscalaServidor(escalaDirecao, diretor), (1, "M"));

            return new Contexto(nb.Id, si.Id, direcao.Id, servidor.Id, colega.Id, servidorSi.Id);
        });

    private static void Marcar(
        CenarioBuilder b,
        EscalaServidor escalaServidor,
        params (int Dia, string Codigo)[] ocorrencias)
    {
        foreach (var (dia, codigo) in ocorrencias)
        {
            b.Db.EscalaOcorrencias.Add(EscalaOcorrencia.Create(
                escalaServidor.Id,
                new DateOnly(Ano, Mes, dia),
                codigo,
                OrigemOcorrencia.Manual,
                createdBy: "teste"));
        }
    }

    private async Task<EscalaCalendarioMesDto> CalendarioAsync(
        string login,
        string escopo,
        Guid? servidorId = null)
    {
        await using var db = NewContext();
        var result = await new EscalaService(db).GetCalendarioMesAsync(
            new EscalaCalendarioMesQuery(Ano, Mes, ServidorId: servidorId, Escopo: escopo),
            login);

        result.Error.ShouldBeNull();
        return result.Value!;
    }

    [Fact]
    public async Task Escopo_setor_traz_so_as_marcacoes_do_setor_que_a_pessoa_chefia()
    {
        var ctx = await PrepararAsync();

        var calendario = await CalendarioAsync(ChefeSetor, "setor");

        calendario.Escalas.Select(x => x.SetorId).ShouldBe([ctx.SetorId]);
        calendario.Itens.Select(x => x.ServidorId).Distinct().Order()
            .ShouldBe(new[] { ctx.ServidorId, ctx.ColegaId }.Order());
        calendario.Itens.ShouldNotContain(x => x.ServidorId == ctx.ServidorDeOutroSetorId);
        calendario.DataInicio.ShouldBe(new DateOnly(Ano, Mes, 1));
        calendario.DataFim.ShouldBe(new DateOnly(Ano, Mes, 31));
    }

    [Fact]
    public async Task Devolve_codigo_categoria_e_nome_de_cada_marcacao()
    {
        var ctx = await PrepararAsync();

        var calendario = await CalendarioAsync(ChefeSetor, "setor");

        var dia1 = calendario.Itens.First(x => x.ServidorId == ctx.ServidorId && x.Data.Day == 1);
        dia1.TipoOcorrenciaCodigo.ShouldBe("M");
        dia1.Categoria.ShouldBe(CategoriaOcorrencia.Trabalho);
        dia1.TipoOcorrenciaNome.ShouldNotBeNullOrWhiteSpace();
        // O domínio normaliza o nome do servidor em maiúsculas.
        dia1.ServidorNome.ShouldBe("ANA PERITOS");
        dia1.SetorId.ShouldBe(ctx.SetorId);

        // Férias e descanso também vêm; é a tela que decide o que mostrar por grupo.
        calendario.Itens
            .Where(x => x.ServidorId == ctx.ServidorId)
            .Select(x => x.TipoOcorrenciaCodigo)
            .ShouldBe(["M", "FR", "D"]);
    }

    [Fact]
    public async Task Escopo_institucional_ve_outros_setores_menos_a_direcao_ic()
    {
        var ctx = await PrepararAsync();

        var calendario = await CalendarioAsync(DirecaoInstitucional, "institucional");

        var setores = calendario.Escalas.Select(x => x.SetorId).ToList();
        setores.ShouldContain(ctx.SetorId);
        setores.ShouldContain(ctx.OutroSetorId);
        setores.ShouldNotContain(ctx.DirecaoSetorId);
        calendario.Itens.ShouldContain(x => x.ServidorId == ctx.ServidorDeOutroSetorId);
    }

    [Fact]
    public async Task Filtra_por_servidor()
    {
        var ctx = await PrepararAsync();

        var calendario = await CalendarioAsync(ChefeSetor, "setor", ctx.ColegaId);

        calendario.Itens.Select(x => x.ServidorId).Distinct().ShouldBe([ctx.ColegaId]);
    }

    [Fact]
    public async Task Quem_nao_chefia_nada_nao_ve_calendario_no_escopo_setor()
    {
        await PrepararAsync();

        var calendario = await CalendarioAsync(SemChefia, "setor");

        calendario.Escalas.ShouldBeEmpty();
        calendario.Itens.ShouldBeEmpty();
    }

    [Fact]
    public async Task Com_duas_versoes_do_mes_usa_a_publicada()
    {
        var ctx = await PrepararAsync();

        // Segunda versão do mesmo setor/mês (permitido enquanto nenhuma está publicada), com
        // outra pessoa marcada, pra garantir que o calendário não soma as duas.
        var segunda = await SemearAsync(b =>
        {
            var setor = b.Db.Setores.First(x => x.Id == ctx.SetorId);
            var escala = b.AdicionarEscala(setor, Ano, Mes);
            var outro = b.AdicionarServidor(setor, "Daniel Segunda Versão");
            Marcar(b, b.AdicionarEscalaServidor(escala, outro), (1, "M"));
            return (EscalaId: escala.Id, ServidorId: outro.Id);
        });

        await using (var db = NewContext())
        {
            var service = new EscalaService(db);
            (await service.FinalizarAsync(segunda.EscalaId, ChefeSetor)).Error.ShouldBeNull();
            (await service.PublicarAsync(
                segunda.EscalaId,
                new PublicarEscalaRequest(ConfirmarConflitos: true),
                ChefeSetor)).Error.ShouldBeNull();
        }

        var calendario = await CalendarioAsync(ChefeSetor, "setor");

        calendario.Escalas.Select(x => x.EscalaId).ShouldBe([segunda.EscalaId]);
        calendario.Escalas.Single().Status.ShouldBe(StatusEscala.Publicada);
        calendario.Itens.Select(x => x.ServidorId).Distinct().ShouldBe([segunda.ServidorId]);
    }

    [Fact]
    public async Task Recusa_mes_invalido()
    {
        await PrepararAsync();

        await using var db = NewContext();
        var result = await new EscalaService(db).GetCalendarioMesAsync(
            new EscalaCalendarioMesQuery(Ano, 13, Escopo: "setor"),
            ChefeSetor);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Mês inválido.");
    }
}
