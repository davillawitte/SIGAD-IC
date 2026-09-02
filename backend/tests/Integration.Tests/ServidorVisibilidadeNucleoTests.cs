using Shouldly;
using TemplateSistema.Application.Common;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Quem chefia só o núcleo (sem chefia direta de nenhum setor) precisa enxergar, em
/// <c>/api/servidores/meus</c>, os servidores lotados nos setores que aquele núcleo
/// engloba — não só os lotados direto no núcleo. Sem isso, o wizard de escala de núcleo
/// (passo "Regimes e servidores") fica sem ninguém pra selecionar.
/// </summary>
public class ServidorVisibilidadeNucleoTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ChefeNucleo = "chefe.nucleo";

    [Fact]
    public async Task Chefe_de_nucleo_sem_chefia_de_setor_ve_servidores_dos_setores_do_nucleo()
    {
        var (servidorDoSetorId, servidorDoNucleoId, servidorDeOutroSetorId) = await SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var chefeServidor = b.AdicionarServidor(direcao, "Chefe do Núcleo");
            b.AdicionarUsuario(chefeServidor, ChefeNucleo, CatalogSeed.PerfilChefeSetorId);

            var nucleo = b.AdicionarNucleo("Núcleo de Perícias", "NPX", chefeServidor.Id);
            var setorDoNucleo = b.AdicionarSetor("Laboratório NPX", "LAB-NPX", nucleo);
            var outroSetor = b.AdicionarSetor("Setor Independente", "SI");

            var servidorDoSetor = b.AdicionarServidor(setorDoNucleo, "Perito do Setor do Núcleo");
            var servidorDoNucleo = b.AdicionarServidorNoNucleo(nucleo, "Servidor Lotado Direto no Núcleo");
            var servidorDeOutroSetor = b.AdicionarServidor(outroSetor, "Servidor de Outro Setor");

            return (servidorDoSetor.Id, servidorDoNucleo.Id, servidorDeOutroSetor.Id);
        });

        await using var db = NewContext();
        var itens = await new ServidorService(db).ListMeusAsync(ChefeNucleo);
        var ids = itens.Select(x => x.Id).ToHashSet();

        // Regressão: antes da correção, servidores lotados num setor do núcleo (não
        // lotados direto nele) não entravam nessa lista pra quem só chefia o núcleo.
        ids.ShouldContain(servidorDoSetorId);
        ids.ShouldContain(servidorDoNucleoId);
        ids.ShouldNotContain(servidorDeOutroSetorId);
    }
}
