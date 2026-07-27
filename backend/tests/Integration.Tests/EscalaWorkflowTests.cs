using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Caracterização da máquina de estados da escala e dos dois fluxos de devolução:
/// setor comum solicita e a Direção IC devolve direto.
/// </summary>
public class EscalaWorkflowTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 7;
    private const string LoginSuper = "superadmin";
    private const string LoginChefe = "chefe.nb";

    private sealed record Contexto(Guid EscalaId, Guid EscalaDirecaoId, Guid SetorId);

    /// <summary>
    /// Dois setores: um comum (NB) e a Direção IC. O chefe do NB é chefia imediata,
    /// e o superadministrador é o único que pode aprovar devoluções neste cenário.
    /// </summary>
    private Task<Contexto> PrepararAsync(TipoFuncionamento tipo = TipoFuncionamento.Expediente) =>
        SemearAsync(b =>
        {
            var setorNb = b.AdicionarSetor("Núcleo de Balística", "NB");
            var chefe = b.AdicionarServidor(setorNb, "Chefe do NB");
            b.AdicionarChefia(setorNb, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, LoginChefe, CatalogSeed.PerfilChefeSetorId);

            var direcao = b.AdicionarDirecaoIc();
            var admin = b.AdicionarServidor(direcao, "Administrador");
            b.AdicionarChefia(setorNb, admin, TipoChefia.ChefiaSubstituta);
            b.AdicionarChefia(direcao, admin, TipoChefia.Diretor);
            b.AdicionarUsuario(
                admin,
                LoginSuper,
                CatalogSeed.PerfilSuperAdminId,
                CatalogSeed.PerfilChefeSetorId,
                CatalogSeed.PerfilDirecaoIcId);

            var escalaNb = b.AdicionarEscala(setorNb, Ano, Mes, tipo);
            var escalaDirecao = b.AdicionarEscala(direcao, Ano, Mes, tipo);

            return new Contexto(escalaNb.Id, escalaDirecao.Id, setorNb.Id);
        });

    private async Task<StatusEscala> StatusAsync(Guid escalaId)
    {
        await using var db = NewContext();
        return await db.Escalas.Where(x => x.Id == escalaId).Select(x => x.Status).FirstAsync();
    }

    private async Task<Result<EscalaDetailDto>> ExecutarAsync(
        Func<EscalaService, Task<Result<EscalaDetailDto>>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    /// <summary>Leva a escala até Publicada, confirmando conflitos.</summary>
    private async Task PublicarAsync(Guid escalaId, string login = LoginSuper)
    {
        (await ExecutarAsync(s => s.FinalizarAsync(escalaId, login))).Succeeded.ShouldBeTrue();
        (await ExecutarAsync(s => s.PublicarAsync(
            escalaId,
            new PublicarEscalaRequest(ConfirmarConflitos: true),
            login))).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Finalizar_move_rascunho_para_finalizada()
    {
        var ctx = await PrepararAsync();

        var resultado = await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper));

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Finalizada);
    }

    [Fact]
    public async Task Finalizar_escala_ja_finalizada_falha()
    {
        var ctx = await PrepararAsync();
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper))).Succeeded.ShouldBeTrue();

        var resultado = await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente escalas em rascunho podem ser finalizadas.");
    }

    [Fact]
    public async Task Reabrir_volta_finalizada_para_rascunho()
    {
        var ctx = await PrepararAsync();
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper))).Succeeded.ShouldBeTrue();

        var resultado = await ExecutarAsync(s => s.ReabrirAsync(ctx.EscalaId, LoginSuper));

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Rascunho);
    }

    [Fact]
    public async Task Reabrir_escala_em_rascunho_falha()
    {
        var ctx = await PrepararAsync();

        var resultado = await ExecutarAsync(s => s.ReabrirAsync(ctx.EscalaId, LoginSuper));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente escalas finalizadas podem voltar para rascunho.");
    }

    [Fact]
    public async Task Publicar_escala_em_rascunho_falha()
    {
        var ctx = await PrepararAsync();

        var resultado = await ExecutarAsync(s => s.PublicarAsync(ctx.EscalaId, null, LoginSuper));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente escalas finalizadas podem ser publicadas.");
    }

    [Fact]
    public async Task Publicar_escala_de_expediente_sem_conflitos_registra_autoria()
    {
        var ctx = await PrepararAsync();
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper))).Succeeded.ShouldBeTrue();

        // Escala de expediente sem ocorrências não gera conflito crítico,
        // então publica sem precisar de confirmação.
        var resultado = await ExecutarAsync(s => s.PublicarAsync(ctx.EscalaId, null, LoginSuper));

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Publicada);

        await using var db = NewContext();
        var escala = await db.Escalas.FirstAsync(x => x.Id == ctx.EscalaId);
        escala.PublicadaPor.ShouldBe(LoginSuper);
        escala.PublicadaEm.ShouldNotBeNull();
    }

    [Fact]
    public async Task Publicar_escala_de_24_horas_sem_cobertura_exige_confirmacao()
    {
        var ctx = await PrepararAsync(TipoFuncionamento.VinteQuatroHoras);
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, LoginSuper))).Succeeded.ShouldBeTrue();

        var semConfirmar = await ExecutarAsync(s => s.PublicarAsync(ctx.EscalaId, null, LoginSuper));

        // Julho tem 31 dias, todos sem cobertura: 31 conflitos críticos.
        semConfirmar.Succeeded.ShouldBeFalse();
        semConfirmar.Error.ShouldBe(
            "Existem 31 conflito(s) crítico(s) na escala. Confirme para publicar mesmo assim.");

        var confirmando = await ExecutarAsync(s => s.PublicarAsync(
            ctx.EscalaId,
            new PublicarEscalaRequest(ConfirmarConflitos: true),
            LoginSuper));

        confirmando.Succeeded.ShouldBeTrue(confirmando.Error);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Publicada);
    }

    [Fact]
    public async Task Solicitar_devolucao_move_publicada_para_devolucao_solicitada()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SolicitarDevolucaoAsync(
            ctx.EscalaId,
            new SolicitarDevolucaoEscalaRequest("Erro na distribuição dos plantões."),
            LoginChefe);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Status.ShouldBe(StatusSolicitacaoDevolucao.Pendente);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.DevolucaoSolicitada);
    }

    [Fact]
    public async Task Solicitar_devolucao_em_escala_nao_publicada_falha()
    {
        var ctx = await PrepararAsync();

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SolicitarDevolucaoAsync(
            ctx.EscalaId,
            new SolicitarDevolucaoEscalaRequest("Justificativa qualquer."),
            LoginChefe);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente escalas publicadas podem solicitar devolução.");
    }

    [Fact]
    public async Task Solicitar_devolucao_sem_justificativa_falha()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SolicitarDevolucaoAsync(
            ctx.EscalaId,
            new SolicitarDevolucaoEscalaRequest("   "),
            LoginChefe);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Justificativa é obrigatória.");
    }

    [Fact]
    public async Task Segunda_solicitacao_de_devolucao_falha_pelo_status_e_nao_pela_pendencia()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        await using (var db = NewContext())
        {
            (await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Primeira."),
                LoginChefe)).Succeeded.ShouldBeTrue();
        }

        await using var segunda = NewContext();
        var resultado = await new EscalaService(segunda).SolicitarDevolucaoAsync(
            ctx.EscalaId,
            new SolicitarDevolucaoEscalaRequest("Segunda."),
            LoginChefe);

        resultado.Succeeded.ShouldBeFalse();

        // A primeira solicitação já move a escala para DevolucaoSolicitada, e a
        // checagem de status vem antes da de pendência. Por isso a mensagem
        // "Já existe uma solicitação de devolução pendente" é inalcançável por este
        // caminho — o que importa registrar é que a segunda tentativa é barrada.
        resultado.Error.ShouldBe("Somente escalas publicadas podem solicitar devolução.");
    }

    [Fact]
    public async Task Escala_da_direcao_ic_nao_solicita_devolucao()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaDirecaoId);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SolicitarDevolucaoAsync(
            ctx.EscalaDirecaoId,
            new SolicitarDevolucaoEscalaRequest("Justificativa."),
            LoginSuper);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A escala da Direção do IC não solicita devolução. Use a ação Devolver.");
    }

    [Fact]
    public async Task Devolucao_direta_funciona_apenas_na_direcao_ic()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaDirecaoId);

        var resultado = await ExecutarAsync(s => s.DevolverAsync(ctx.EscalaDirecaoId, LoginSuper));

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        (await StatusAsync(ctx.EscalaDirecaoId)).ShouldBe(StatusEscala.Finalizada);
    }

    [Fact]
    public async Task Devolucao_direta_em_setor_comum_falha()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId);

        var resultado = await ExecutarAsync(s => s.DevolverAsync(ctx.EscalaId, LoginSuper));

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("A devolução direta só é permitida para a escala da Direção do IC.");
    }

    [Fact]
    public async Task Aprovar_devolucao_finaliza_a_escala()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        Guid solicitacaoId;
        await using (var db = NewContext())
        {
            var solicitacao = await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                LoginChefe);
            solicitacao.Succeeded.ShouldBeTrue(solicitacao.Error);
            solicitacaoId = solicitacao.Value!.Id;
        }

        await using var aprovacao = NewContext();
        var resultado = await new EscalaService(aprovacao).AprovarDevolucaoAsync(
            solicitacaoId,
            new ResponderDevolucaoEscalaRequest("Pode corrigir."),
            LoginSuper);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Status.ShouldBe(StatusSolicitacaoDevolucao.Aprovada);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Finalizada);
    }

    [Fact]
    public async Task Recusar_devolucao_devolve_a_escala_para_publicada()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        Guid solicitacaoId;
        await using (var db = NewContext())
        {
            var solicitacao = await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                LoginChefe);
            solicitacaoId = solicitacao.Value!.Id;
        }

        await using var recusa = NewContext();
        var resultado = await new EscalaService(recusa).RecusarDevolucaoAsync(
            solicitacaoId,
            new ResponderDevolucaoEscalaRequest("Sem motivo."),
            LoginSuper);

        resultado.Succeeded.ShouldBeTrue(resultado.Error);
        resultado.Value!.Status.ShouldBe(StatusSolicitacaoDevolucao.Recusada);
        (await StatusAsync(ctx.EscalaId)).ShouldBe(StatusEscala.Publicada);
    }

    [Fact]
    public async Task Responder_solicitacao_ja_respondida_falha()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        Guid solicitacaoId;
        await using (var db = NewContext())
        {
            var solicitacao = await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                LoginChefe);
            solicitacaoId = solicitacao.Value!.Id;
        }

        await using (var db = NewContext())
        {
            (await new EscalaService(db).AprovarDevolucaoAsync(solicitacaoId, null, LoginSuper))
                .Succeeded.ShouldBeTrue();
        }

        await using var segunda = NewContext();
        var resultado = await new EscalaService(segunda).AprovarDevolucaoAsync(
            solicitacaoId,
            null,
            LoginSuper);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Somente solicitações pendentes podem ser respondidas.");
    }

    [Fact]
    public async Task Chefe_de_setor_nao_responde_devolucoes()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        Guid solicitacaoId;
        await using (var db = NewContext())
        {
            var solicitacao = await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                LoginChefe);
            solicitacaoId = solicitacao.Value!.Id;
        }

        await using var db2 = NewContext();
        var resultado = await new EscalaService(db2).AprovarDevolucaoAsync(solicitacaoId, null, LoginChefe);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Sem permissão para responder devoluções.");
    }

    [Fact]
    public async Task Devolucoes_pendentes_ficam_vazias_para_quem_nao_pode_aprovar()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        await using (var db = NewContext())
        {
            (await new EscalaService(db).SolicitarDevolucaoAsync(
                ctx.EscalaId,
                new SolicitarDevolucaoEscalaRequest("Preciso corrigir."),
                LoginChefe)).Succeeded.ShouldBeTrue();
        }

        await using var db2 = NewContext();
        var service = new EscalaService(db2);

        // O chefe recebe lista vazia em vez de erro.
        (await service.ListDevolucoesPendentesAsync(LoginChefe)).ShouldBeEmpty();
        (await service.ListDevolucoesPendentesAsync(LoginSuper)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Solicitar_devolucao_com_login_inexistente_falha()
    {
        var ctx = await PrepararAsync();
        await PublicarAsync(ctx.EscalaId, LoginChefe);

        await using var db = NewContext();
        var resultado = await new EscalaService(db).SolicitarDevolucaoAsync(
            ctx.EscalaId,
            new SolicitarDevolucaoEscalaRequest("Justificativa."),
            "nao.existe");

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Usuário não encontrado.");
    }
}
