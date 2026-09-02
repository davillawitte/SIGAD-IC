using Shouldly;
using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Autorização por matriz: visão/devolução vêm do perfil Direção IC (TodosOsSetores +
/// escalas.devolver). Mutação de escala/afastamento exige permissão de escrita no mesmo
/// perfil cuja abrangência cobre o setor — combinação Direção+Chefe não escala escrita.
/// </summary>
public class AutorizacaoPorSetorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 7;

    private const string Super = "superadmin";
    private const string Diretor = "diretor.ic";
    private const string Subcoordenador = "subcoordenador.ic";
    private const string ChefeNb = "chefe.nb";
    private const string SemChefia = "sem.chefia";
    private const string Inexistente = "nao.existe";

    private sealed record Contexto(
        Guid EscalaNbId,
        Guid EscalaNpId,
        Guid EscalaDirecaoId,
        Guid ServidorNbId,
        Guid ServidorNpId,
        Guid AfastamentoNbId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var nb = b.AdicionarSetor("Núcleo de Balística", "NB");
            var np = b.AdicionarSetor("Núcleo de Papiloscopia", "NP");

            var admin = b.AdicionarServidor(direcao, "Administrador");
            b.AdicionarChefia(direcao, admin, TipoChefia.ChefiaSubstituta);
            // SuperAdmin (admin do sistema) + Direção IC (visão/devolver global, mutar Meus).
            b.AdicionarSuperAdmin(admin, Super, CatalogSeed.PerfilDirecaoIcId);

            // Diretor e Subcoordenador: perfil Direção IC (visão + devolver) + Chefe
            // (escrita só nos setores gerenciados — anti-escalonamento por perfil).
            var diretor = b.AdicionarServidor(direcao, "Diretor");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarUsuario(diretor, Diretor, CatalogSeed.PerfilChefeSetorId, CatalogSeed.PerfilDirecaoIcId);

            var sub = b.AdicionarServidor(direcao, "Subcoordenador");
            b.AdicionarChefia(direcao, sub, TipoChefia.Subcoordenador);
            b.AdicionarUsuario(sub, Subcoordenador, CatalogSeed.PerfilChefeSetorId, CatalogSeed.PerfilDirecaoIcId);

            var chefeNb = b.AdicionarServidor(nb, "Chefe do NB");
            b.AdicionarChefia(nb, chefeNb, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefeNb, ChefeNb, CatalogSeed.PerfilChefeSetorId);

            var comum = b.AdicionarServidor(nb, "Servidor Comum");
            b.AdicionarUsuario(comum, SemChefia, CatalogSeed.PerfilServidorId);

            var servidorNb = b.AdicionarServidor(nb, "Perito do NB");
            var servidorNp = b.AdicionarServidor(np, "Perito do NP");

            var afastamentoNb = b.AdicionarAfastamento(
                servidorNb,
                new DateOnly(Ano, Mes, 10),
                new DateOnly(Ano, Mes, 15));

            return new Contexto(
                b.AdicionarEscala(nb, Ano, Mes).Id,
                b.AdicionarEscala(np, Ano, Mes).Id,
                b.AdicionarEscala(direcao, Ano, Mes).Id,
                servidorNb.Id,
                servidorNp.Id,
                afastamentoNb.Id);
        });

    private async Task<bool> PodeVerEscalaAsync(Guid escalaId, string login)
    {
        await using var db = NewContext();
        return (await new EscalaService(db).GetByIdAsync(escalaId, login)).Succeeded;
    }

    private async Task<bool> PodeMutarEscalaAsync(Guid escalaId, string login)
    {
        await using var db = NewContext();
        return (await new EscalaService(db).FinalizarAsync(escalaId, login)).Succeeded;
    }

    private async Task<int> EscalasVisiveisAsync(string login, string? escopo = null)
    {
        await using var db = NewContext();
        return (await new EscalaService(db).ListAsync(new EscalaListQuery { Escopo = escopo }, login))
            .TotalItems;
    }

    private async Task<int> AfastamentosVisiveisAsync(string login, string? escopo = null)
    {
        await using var db = NewContext();
        return (await new AfastamentoService(db).ListAsync(new AfastamentoListQuery { Escopo = escopo }, login))
            .Count;
    }

    [Fact]
    public async Task Listagem_setor_so_chefia_e_institucional_exclui_direcao_ic()
    {
        var ctx = await PrepararAsync();

        // Gestão do Setor: Diretor só vê a escala da própria Direção IC (onde é chefia).
        (await EscalasVisiveisAsync(Diretor, "setor")).ShouldBe(1);
        (await EscalasVisiveisAsync(ChefeNb, "setor")).ShouldBe(1);

        // Gestão Institucional: demais setores, sem a Direção IC.
        (await EscalasVisiveisAsync(Diretor, "institucional")).ShouldBe(2);
        (await EscalasVisiveisAsync(ChefeNb, "institucional")).ShouldBe(0);

        (await AfastamentosVisiveisAsync(Diretor, "setor")).ShouldBe(0);
        (await AfastamentosVisiveisAsync(ChefeNb, "setor")).ShouldBe(1);
        (await AfastamentosVisiveisAsync(Diretor, "institucional")).ShouldBe(1);
        (await AfastamentosVisiveisAsync(ChefeNb, "institucional")).ShouldBe(0);

        // Detalhe: a escala NB continua acessível ao Diretor (visão), só não entra na listagem de setor.
        (await PodeVerEscalaAsync(ctx.EscalaNbId, Diretor)).ShouldBeTrue();
        (await PodeVerEscalaAsync(ctx.EscalaDirecaoId, Diretor)).ShouldBeTrue();
    }

    [Fact]
    public async Task Superadministrador_ve_tudo_mas_so_muta_setores_gerenciados()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, Super)).ShouldBeTrue();
        (await PodeVerEscalaAsync(ctx.EscalaNpId, Super)).ShouldBeTrue();
        (await EscalasVisiveisAsync(Super)).ShouldBe(3);

        // Poder operacional vem do perfil Direção IC (Mutar = MeusSetores).
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, Super)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaDirecaoId, Super)).ShouldBeTrue();
    }

    [Fact]
    public async Task Superadministrador_nao_solicita_devolucao_fora_do_setor_gerenciado()
    {
        var ctx = await PrepararAsync();

        await using (var db = NewContext())
        {
            var service = new EscalaService(db);
            (await service.FinalizarAsync(ctx.EscalaNbId, ChefeNb)).Succeeded.ShouldBeTrue();
            (await service.PublicarAsync(
                ctx.EscalaNbId,
                new PublicarEscalaRequest(ConfirmarConflitos: true),
                ChefeNb)).Succeeded.ShouldBeTrue();
        }

        await using var db2 = NewContext();
        var resultado = await new EscalaService(db2).SolicitarDevolucaoAsync(
            ctx.EscalaNbId,
            new SolicitarDevolucaoEscalaRequest("Tentativa indevida."),
            Super);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Sem permissão para esta escala.");
    }

    [Fact]
    public async Task Perfil_unico_direcao_ic_ve_tudo_e_muta_so_o_setor_gerenciado()
    {
        // Um único perfil com Ver=Todos e Mutar=Meus (sem CHEFE_SETOR separado).
        var ctx = await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var nb = b.AdicionarSetor("Núcleo de Balística", "NB");

            var diretor = b.AdicionarServidor(direcao, "Diretor Solo");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarUsuario(diretor, "diretor.solo", CatalogSeed.PerfilDirecaoIcId);

            return new Contexto(
                b.AdicionarEscala(nb, Ano, Mes).Id,
                Guid.Empty,
                b.AdicionarEscala(direcao, Ano, Mes).Id,
                Guid.Empty,
                Guid.Empty,
                Guid.Empty);
        });

        (await PodeVerEscalaAsync(ctx.EscalaNbId, "diretor.solo")).ShouldBeTrue();
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, "diretor.solo")).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaDirecaoId, "diretor.solo")).ShouldBeTrue();
    }

    [Fact]
    public async Task Diretor_da_direcao_ic_tem_visao_global_mas_muta_somente_o_que_gerencia()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, Diretor)).ShouldBeTrue();
        (await PodeVerEscalaAsync(ctx.EscalaNpId, Diretor)).ShouldBeTrue();
        (await EscalasVisiveisAsync(Diretor)).ShouldBe(3);

        // Anti-escalonamento: DIRECAO_IC tem listar (Todos) mas escrita está no CHEFE (Meus).
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, Diretor)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaDirecaoId, Diretor)).ShouldBeTrue();
    }

    [Fact]
    public async Task Subcoordenador_com_perfil_direcao_ic_tem_mesma_visao_global_que_diretor()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, Subcoordenador)).ShouldBeTrue();
        (await PodeVerEscalaAsync(ctx.EscalaNpId, Subcoordenador)).ShouldBeTrue();
        (await EscalasVisiveisAsync(Subcoordenador)).ShouldBe(3);

        (await PodeMutarEscalaAsync(ctx.EscalaNbId, Subcoordenador)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaDirecaoId, Subcoordenador)).ShouldBeTrue();
    }

    [Fact]
    public async Task Chefe_de_setor_fica_restrito_ao_proprio_setor()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, ChefeNb)).ShouldBeTrue();
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, ChefeNb)).ShouldBeTrue();

        (await PodeVerEscalaAsync(ctx.EscalaNpId, ChefeNb)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaNpId, ChefeNb)).ShouldBeFalse();
        (await EscalasVisiveisAsync(ChefeNb)).ShouldBe(1);
    }

    [Fact]
    public async Task Servidor_sem_chefia_nao_ve_nem_muta_escalas()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, SemChefia)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, SemChefia)).ShouldBeFalse();
        (await EscalasVisiveisAsync(SemChefia)).ShouldBe(0);
    }

    [Fact]
    public async Task Login_inexistente_nao_ve_nem_muta_escalas()
    {
        var ctx = await PrepararAsync();

        (await PodeVerEscalaAsync(ctx.EscalaNbId, Inexistente)).ShouldBeFalse();
        (await PodeMutarEscalaAsync(ctx.EscalaNbId, Inexistente)).ShouldBeFalse();
        (await EscalasVisiveisAsync(Inexistente)).ShouldBe(0);
    }

    [Fact]
    public async Task Login_e_normalizado_antes_da_busca()
    {
        var ctx = await PrepararAsync();
        (await PodeVerEscalaAsync(ctx.EscalaNbId, "  CHEFE.NB  ")).ShouldBeTrue();
    }

    [Fact]
    public async Task Criar_escala_respeita_o_setor_gerenciado()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaService(db);
        var setorNpId = (await service.GetByIdAsync(ctx.EscalaNpId, Super)).Value!.SetorId;

        var negado = await service.CreateAsync(
            new CreateEscalaRequest(setorNpId, null, Ano, 9, TipoFuncionamento.Expediente, null),
            ChefeNb);

        negado.Succeeded.ShouldBeFalse();
        negado.Error.ShouldBe("Sem permissão para criar escala neste setor.");
    }

    [Fact]
    public async Task Aprovacao_de_devolucao_e_de_quem_tem_escalas_devolver()
    {
        var ctx = await PrepararAsync();

        await using (var db = NewContext())
        {
            var service = new EscalaService(db);
            (await service.FinalizarAsync(ctx.EscalaNbId, ChefeNb)).Succeeded.ShouldBeTrue();
            (await service.PublicarAsync(
                ctx.EscalaNbId,
                new PublicarEscalaRequest(ConfirmarConflitos: true),
                ChefeNb)).Succeeded.ShouldBeTrue();
            (await service.SolicitarDevolucaoAsync(
                ctx.EscalaNbId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                ChefeNb)).Succeeded.ShouldBeTrue();
        }

        await using var leitura = NewContext();
        var escalas = new EscalaService(leitura);

        (await escalas.ListDevolucoesPendentesAsync(Super)).Count.ShouldBe(1);
        (await escalas.ListDevolucoesPendentesAsync(Diretor)).Count.ShouldBe(1);
        (await escalas.ListDevolucoesPendentesAsync(Subcoordenador)).Count.ShouldBe(1);
        (await escalas.ListDevolucoesPendentesAsync(ChefeNb)).ShouldBeEmpty();
        (await escalas.ListDevolucoesPendentesAsync(SemChefia)).ShouldBeEmpty();
    }

    private async Task<bool> PodeVerAfastamentoAsync(Guid afastamentoId, string login)
    {
        await using var db = NewContext();
        return (await new AfastamentoService(db).GetByIdAsync(afastamentoId, login)).Succeeded;
    }

    [Fact]
    public async Task Afastamento_superadministrador_ve_tudo()
    {
        var ctx = await PrepararAsync();

        (await PodeVerAfastamentoAsync(ctx.AfastamentoNbId, Super)).ShouldBeTrue();
        (await AfastamentosVisiveisAsync(Super)).ShouldBe(1);
    }

    [Fact]
    public async Task Afastamento_diretor_e_subcoordenador_tem_visao_global()
    {
        var ctx = await PrepararAsync();

        (await PodeVerAfastamentoAsync(ctx.AfastamentoNbId, Diretor)).ShouldBeTrue();
        (await PodeVerAfastamentoAsync(ctx.AfastamentoNbId, Subcoordenador)).ShouldBeTrue();
        (await AfastamentosVisiveisAsync(Diretor)).ShouldBe(1);
        (await AfastamentosVisiveisAsync(Subcoordenador)).ShouldBe(1);
    }

    [Fact]
    public async Task Afastamento_chefe_ve_apenas_o_proprio_setor()
    {
        var ctx = await PrepararAsync();

        (await PodeVerAfastamentoAsync(ctx.AfastamentoNbId, ChefeNb)).ShouldBeTrue();
        (await AfastamentosVisiveisAsync(ChefeNb)).ShouldBe(1);
        (await AfastamentosVisiveisAsync(SemChefia)).ShouldBe(0);
        (await AfastamentosVisiveisAsync(Inexistente)).ShouldBe(0);
    }

    [Fact]
    public async Task Afastamento_so_pode_ser_criado_para_servidor_do_setor_gerenciado()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var service = new AfastamentoService(db);

        var permitido = await service.CreateAsync(
            new CreateAfastamentoRequest(
                ctx.ServidorNbId,
                new DateOnly(Ano, 8, 1),
                new DateOnly(Ano, 8, 5),
                "LM",
                null),
            ChefeNb);

        permitido.Succeeded.ShouldBeTrue(permitido.Error);

        var negado = await service.CreateAsync(
            new CreateAfastamentoRequest(
                ctx.ServidorNpId,
                new DateOnly(Ano, 8, 1),
                new DateOnly(Ano, 8, 5),
                "LM",
                null),
            ChefeNb);

        negado.Succeeded.ShouldBeFalse();
        negado.Error.ShouldBe("Só é possível cadastrar afastamento para servidores do setor em que você é chefe.");
    }

    [Fact]
    public async Task Afastamento_direcao_ic_nao_cria_fora_do_setor_gerenciado()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new AfastamentoService(db).CreateAsync(
            new CreateAfastamentoRequest(
                ctx.ServidorNbId,
                new DateOnly(Ano, 8, 1),
                new DateOnly(Ano, 8, 5),
                "LM",
                null),
            Diretor);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Só é possível cadastrar afastamento para servidores do setor em que você é chefe.");
    }
}
