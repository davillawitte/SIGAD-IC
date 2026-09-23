using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Chefe de núcleo (sem chefia direta do setor) sobre a escala de um setor que o núcleo engloba.
/// Mutar já era permitido, mas <c>CanView</c> não considerava a chefia de núcleo: ler a escala
/// falhava com "sem permissão" e a cópia chegava a ser gravada antes disso, deixando escala órfã.
/// </summary>
public class ChefeNucleoEscalaDeSetorTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string ChefeNucleo = "chefe.nucleo";
    private const string DirecaoInstitucional = "direcao.institucional";

    private sealed record Contexto(Guid SetorId, Guid EscalaId, Guid ServidorId);

    /// <summary>Núcleo NPE com o setor SCCV; o chefe do núcleo não tem chefia direta do setor.</summary>
    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            var nucleo = b.AdicionarNucleo("Núcleo de Perícias Externas", "NPE", chefe.Id);
            var setor = b.AdicionarSetor("Setor de Crimes Contra a Vida", "SCCV", nucleo);
            var servidor = b.AdicionarServidor(setor, "Servidor do Setor");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            // Visão institucional (Direção IC + superadministrador), sem chefia do setor/núcleo.
            var diretor = b.AdicionarServidor(direcao, "Diretor do IC");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarSuperAdmin(
                diretor, DirecaoInstitucional, CatalogSeed.PerfilDirecaoIcId, CatalogSeed.PerfilChefeSetorId);

            var escala = b.AdicionarEscala(setor, Ano, Mes);
            b.AdicionarEscalaServidor(escala, servidor);

            return new Contexto(setor.Id, escala.Id, servidor.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<EscalaService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new EscalaService(db));
    }

    [Fact]
    public async Task Le_a_escala_de_setor_do_nucleo_que_chefia()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.GetByIdAsync(ctx.EscalaId, ChefeNucleo));

        result.Error.ShouldBeNull();
        result.Value!.Id.ShouldBe(ctx.EscalaId);
    }

    [Fact]
    public async Task Cria_escala_para_setor_do_nucleo_que_chefia()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.CreateAsync(
            new CreateEscalaRequest(ctx.SetorId, null, Ano, Mes + 1, TipoFuncionamento.Expediente, null),
            ChefeNucleo));

        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task Visao_institucional_ve_mas_nao_edita_escala_de_outro_setor()
    {
        var ctx = await PrepararAsync();

        var leitura = await ExecutarAsync(s => s.GetByIdAsync(ctx.EscalaId, DirecaoInstitucional));
        var edicao = await ExecutarAsync(s => s.UpdateAsync(
            ctx.EscalaId,
            new UpdateEscalaRequest(Ano, Mes, TipoFuncionamento.Expediente, "tentativa"),
            DirecaoInstitucional));
        var exclusao = await ExecutarAsync(s => s.DeleteAsync(ctx.EscalaId, DirecaoInstitucional));

        leitura.Error.ShouldBeNull();
        edicao.Succeeded.ShouldBeFalse();
        edicao.Error.ShouldBe("Sem permissão para esta escala.");
        exclusao.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Copia_escala_publicada_do_setor_do_nucleo_que_chefia()
    {
        var ctx = await PrepararAsync();
        (await ExecutarAsync(s => s.FinalizarAsync(ctx.EscalaId, ChefeNucleo))).Error.ShouldBeNull();
        (await ExecutarAsync(s => s.PublicarAsync(
            ctx.EscalaId, new PublicarEscalaRequest(ConfirmarConflitos: true), ChefeNucleo))).Error.ShouldBeNull();

        var result = await ExecutarAsync(s => s.CopiarAsync(
            ctx.EscalaId, new CopiarEscalaRequest(Ano, Mes + 1), ChefeNucleo));

        result.Error.ShouldBeNull();
        result.Value!.Servidores.Select(x => x.ServidorId).ShouldBe([ctx.ServidorId]);
        await using var db = NewContext();
        (await db.Escalas.CountAsync(x => x.SetorId == ctx.SetorId && x.Mes == Mes + 1)).ShouldBe(1);
    }
}
