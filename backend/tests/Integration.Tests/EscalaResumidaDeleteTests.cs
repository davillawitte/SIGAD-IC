using Shouldly;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Excluir uma escala resumida ainda não vinculada a nenhuma escala real é o mecanismo usado
/// pelo wizard de escala pra descartar rascunhos órfãos (criados ao abrir o passo opcional,
/// mas nunca "adotados" por uma escala salva) — ver `EscalaForm.cleanupResumidaOrfa$` e
/// `descartarResumida` no frontend. Uma vez vinculada (`VincularEscalaAsync`), a escala
/// resumida passa a ser compartilhada pela escala real e não pode mais ser apagada por baixo
/// dela.
/// </summary>
public class EscalaResumidaDeleteTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo.delete";

    private async Task<(Guid NucleoId, Guid SetorId)> PrepararAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Delete");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Delete", "NDL", chefe.Id);
            var setor = b.AdicionarSetor("Setor Delete", "SDL", nucleo);

            return (nucleo.Id, setor.Id);
        });

    private async Task<(Guid NucleoId, Guid EscalaId)> PrepararComEscalaRealAsync() =>
        await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefe = b.AdicionarServidor(direcao, "Chefe do Núcleo Delete Vinculada");
            b.AdicionarUsuario(chefe, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo Delete Vinculada", "NDV", chefe.Id);
            var setor = b.AdicionarSetor("Setor Delete Vinculada", "SDV", nucleo);
            var escala = b.AdicionarEscala(setor, 2026, 3);

            return (nucleo.Id, escala.Id);
        });

    [Fact]
    public async Task Excluir_escala_resumida_nao_vinculada_remove_e_cascateia_os_filhos()
    {
        var (nucleoId, setorId) = await PrepararAsync();

        await using var db = NewContext();
        var service = new EscalaResumidaService(db);

        var criada = (await service.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 3, null), ChefeNucleo)).Value!;
        var comSetor = (await service.ConfigurarSetoresAsync(
            criada.Id, new ConfigurarSetoresRequest([new ConfigurarSetorItem(setorId, 1)]), ChefeNucleo)).Value!;
        await service.ConfigurarEquipeAsync(
            criada.Id,
            new ConfigurarEquipeRequest(comSetor.Setores[0].Id),
            ChefeNucleo);

        var resultado = await service.DeleteAsync(criada.Id, ChefeNucleo);

        resultado.Succeeded.ShouldBeTrue();
        (await db.EscalasResumidas.FindAsync(criada.Id)).ShouldBeNull();
        db.EscalaResumidaSetores.Any(s => s.EscalaResumidaId == criada.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Excluir_escala_resumida_ja_vinculada_a_uma_escala_falha()
    {
        var (nucleoId, escalaId) = await PrepararComEscalaRealAsync();

        await using var db = NewContext();
        var resumidaService = new EscalaResumidaService(db);

        var criada = (await resumidaService.CreateAsync(
            new CreateEscalaResumidaRequest(nucleoId, 2026, 3, null), ChefeNucleo)).Value!;

        await resumidaService.VincularEscalaAsync(criada.Id, escalaId, ChefeNucleo);

        var resultado = await resumidaService.DeleteAsync(criada.Id, ChefeNucleo);

        resultado.Succeeded.ShouldBeFalse();
        resultado.Error.ShouldBe("Escala resumida já vinculada a uma escala salva — não pode ser excluída.");
        (await db.EscalasResumidas.FindAsync(criada.Id)).ShouldNotBeNull();
    }
}
