using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Só a escala PUBLICADA ocupa o mês: rascunhos/finalizadas do mesmo setor+mês coexistem como
/// versões da mesma escala. Antes o índice único do banco valia para qualquer status e criar/
/// copiar/salvar uma segunda versão estourava 500 ("Operação não concluída." na tela).
/// </summary>
public class EscalaVersoesMesmoMesTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Login = "superadmin";

    private sealed record Contexto(Guid SetorId, Guid OutroSetorId, Guid EscalaId, Guid ServidorId);

    /// <summary>Setor NB com uma escala em rascunho (com o servidor Ricardo) e um segundo setor.</summary>
    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setorNb = b.AdicionarSetor("Núcleo de Balística", "NB");
            var outro = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV");
            var ricardo = b.AdicionarServidor(setorNb, "Ricardo Oliveira");

            var direcao = b.AdicionarDirecaoIc();
            var admin = b.AdicionarServidor(direcao, "Administrador");
            b.AdicionarChefia(setorNb, admin, TipoChefia.ChefiaSubstituta);
            b.AdicionarChefia(direcao, admin, TipoChefia.Diretor);
            b.AdicionarUsuario(
                admin,
                Login,
                CatalogSeed.PerfilSuperAdminId,
                CatalogSeed.PerfilChefeSetorId,
                CatalogSeed.PerfilDirecaoIcId);

            var escala = b.AdicionarEscala(setorNb, Ano, Mes);
            b.AdicionarEscalaServidor(escala, ricardo);

            return new Contexto(setorNb.Id, outro.Id, escala.Id, ricardo.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    private Task<Result<EscalaDetailDto>> CriarAsync(Guid setorId) =>
        ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(setorId, null, Ano, Mes, TipoFuncionamento.Expediente, null), Login));

    private async Task<Result<EscalaDetailDto>> PublicarAsync(Guid escalaId)
    {
        var finalizada = await ExecutarAsync(s => s.FinalizarAsync(escalaId, Login));
        finalizada.Error.ShouldBeNull();
        return await ExecutarAsync(s => s.PublicarAsync(
            escalaId, new PublicarEscalaRequest(ConfirmarConflitos: true), Login));
    }

    [Fact]
    public async Task Criar_segunda_versao_no_mesmo_mes_com_rascunho_existente()
    {
        var ctx = await PrepararAsync();

        var result = await CriarAsync(ctx.SetorId);

        result.Error.ShouldBeNull();
        await using var db = NewContext();
        (await db.Escalas.CountAsync(x => x.SetorId == ctx.SetorId && x.Ano == Ano && x.Mes == Mes)).ShouldBe(2);
    }

    [Fact]
    public async Task Copiar_para_mes_que_ja_tem_rascunho_do_mesmo_setor()
    {
        var ctx = await PrepararAsync();
        var rascunhoNoDestino = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(ctx.SetorId, null, Ano, Mes + 1, TipoFuncionamento.Expediente, null), Login));
        rascunhoNoDestino.Error.ShouldBeNull();
        (await PublicarAsync(ctx.EscalaId)).Error.ShouldBeNull();

        var result = await ExecutarAsync(s => s.CopiarAsync(
            ctx.EscalaId, new CopiarEscalaRequest(Ano, Mes + 1), Login));

        result.Error.ShouldBeNull();
        result.Value!.Servidores.Select(x => x.ServidorId).ShouldBe([ctx.ServidorId]);
    }

    [Fact]
    public async Task Copiar_escala_nao_publicada_e_recusado()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.CopiarAsync(
            ctx.EscalaId, new CopiarEscalaRequest(Ano, Mes + 1), Login));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(
            "Só é possível copiar escalas publicadas. Publique a escala de origem antes de copiá-la.");
        await using var db = NewContext();
        // Nada pode ter sido gravado no mês de destino.
        (await db.Escalas.CountAsync(x => x.SetorId == ctx.SetorId && x.Mes == Mes + 1)).ShouldBe(0);
    }

    [Fact]
    public async Task Mesmo_servidor_em_duas_versoes_do_mesmo_setor_nao_conflita()
    {
        var ctx = await PrepararAsync();
        var segunda = await CriarAsync(ctx.SetorId);

        var result = await ExecutarAsync(s => s.AddServidoresAsync(
            segunda.Value!.Id, new AddEscalaServidoresRequest([ctx.ServidorId]), Login));

        result.Error.ShouldBeNull();
        var conflitos = await ExecutarAsync(s => s.CheckConflitosServidoresAsync(
            new CheckConflitosServidoresRequest(Ano, Mes, [ctx.ServidorId], SetorId: ctx.SetorId), Login));
        conflitos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Servidor_em_escala_de_outro_setor_continua_conflitando()
    {
        var ctx = await PrepararAsync();

        var conflitos = await ExecutarAsync(s => s.CheckConflitosServidoresAsync(
            new CheckConflitosServidoresRequest(Ano, Mes, [ctx.ServidorId], SetorId: ctx.OutroSetorId), Login));

        conflitos.Select(x => x.ServidorId).ShouldBe([ctx.ServidorId]);
        conflitos[0].Origem.ShouldBe("escala do setor NB");
    }

    [Fact]
    public async Task Segunda_versao_nao_pode_ser_publicada_quando_ja_ha_publicada()
    {
        var ctx = await PrepararAsync();
        var segunda = await CriarAsync(ctx.SetorId);
        (await PublicarAsync(ctx.EscalaId)).Error.ShouldBeNull();

        var result = await PublicarAsync(segunda.Value!.Id);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Já existe escala publicada sobreposta neste setor/núcleo e período.");
    }

    /// <summary>
    /// Copiar uma escala que tem jornada gerada E ocorrência manual no mesmo dia estourava
    /// 23505 em IX_EscalaOcorrencia_EscalaServidorId_Data: as ocorrências da última jornada
    /// ainda estavam pendentes no contexto quando a manual era procurada no banco.
    /// </summary>
    [Fact]
    public async Task Copiar_escala_com_ocorrencia_manual_sobre_dia_de_jornada()
    {
        var ctx = await PrepararAsync();
        var padrao12x36 = await PadraoIdAsync("12X36");
        var primeiroDia = new DateOnly(Ano, Mes, 1);

        // Escala de plantão (não administrativa): aqui a cópia leva a marcação manual pela data,
        // sem a reprodução da semana que vale para o expediente.
        var escalaPlantao = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(ctx.SetorId, null, Ano, Mes, TipoFuncionamento.VinteQuatroHoras, null),
            Login));
        escalaPlantao.Error.ShouldBeNull();
        var escalaId = escalaPlantao.Value!.Id;
        (await ExecutarAsync(s => s.AddServidoresAsync(
            escalaId, new AddEscalaServidoresRequest([ctx.ServidorId]), Login))).Error.ShouldBeNull();

        var gerada = await ExecutarAsync(s => s.GerarEscalaAsync(
            escalaId,
            new GerarEscalaRequest(
                [new GerarEscalaItemRequest(ctx.ServidorId, padrao12x36, primeiroDia, null, null)],
                DistribuirAutomaticamente: false,
                DataBaseDistribuicao: null),
            Login));
        gerada.Error.ShouldBeNull();

        // Ocorrência manual por cima de um dia que a jornada 12x36 já preencheu. Usa teletrabalho
        // (e não férias) porque afastamento não é copiado — vem do cadastro (ver
        // CopiarEscalaComAfastamentoTests).
        var manual = await ExecutarAsync(s => s.UpsertOcorrenciaAsync(
            escalaId,
            ctx.ServidorId,
            new UpsertOcorrenciaRequest(primeiroDia, "TL12", null, null, null, "Laudo"),
            Login));
        manual.Error.ShouldBeNull();
        (await PublicarAsync(escalaId)).Error.ShouldBeNull();

        var copia = await ExecutarAsync(s => s.CopiarAsync(
            escalaId, new CopiarEscalaRequest(Ano, Mes + 1), Login));

        copia.Error.ShouldBeNull();
        await using var db = NewContext();
        var ocorrenciasDoDia = await db.EscalaOcorrencias
            .Where(x => x.EscalaServidor.EscalaId == copia.Value!.Id
                        && x.Data == new DateOnly(Ano, Mes + 1, 1))
            .ToListAsync();
        ocorrenciasDoDia.Count.ShouldBe(1);
        ocorrenciasDoDia[0].TipoOcorrenciaCodigo.ShouldBe("TL12");
    }

    [Fact]
    public async Task Listagem_ordena_por_periodo_e_desempata_pela_mais_recente()
    {
        var ctx = await PrepararAsync();
        var segundaDoMes = await CriarAsync(ctx.SetorId);
        var mesSeguinte = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(ctx.SetorId, null, Ano, Mes + 1, TipoFuncionamento.Expediente, null), Login));
        mesSeguinte.Error.ShouldBeNull();

        var desc = await ExecutarAsync(s => s.ListAsync(
            new EscalaListQuery { SetorId = ctx.SetorId, PageSize = 50 }, Login));
        var asc = await ExecutarAsync(s => s.ListAsync(
            new EscalaListQuery { SetorId = ctx.SetorId, PageSize = 50, Sort = "periodo", Dir = "asc" }, Login));

        // Padrão: período mais recente primeiro e, no mesmo mês, a criada por último.
        desc.Items.Select(x => x.Id).ShouldBe([mesSeguinte.Value!.Id, segundaDoMes.Value!.Id, ctx.EscalaId]);
        asc.Items.Select(x => x.Id).ShouldBe([segundaDoMes.Value!.Id, ctx.EscalaId, mesSeguinte.Value!.Id]);
    }

    [Fact]
    public async Task Escala_anterior_prefere_a_versao_publicada()
    {
        var ctx = await PrepararAsync();
        var segunda = await CriarAsync(ctx.SetorId);
        (await PublicarAsync(ctx.EscalaId)).Error.ShouldBeNull();

        var anterior = await ExecutarAsync(s => s.GetEscalaAnteriorAsync(ctx.SetorId, null, Ano, Mes + 1, Login));

        anterior.ShouldNotBeNull();
        anterior.Id.ShouldBe(ctx.EscalaId);
        segunda.Value!.Id.ShouldNotBe(ctx.EscalaId);
    }
}
