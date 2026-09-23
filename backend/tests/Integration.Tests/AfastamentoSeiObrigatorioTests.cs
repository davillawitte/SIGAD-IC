using Shouldly;
using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// O número do processo SEI é obrigatório no afastamento: sem ele não há como conferir depois a
/// concessão. Vale para o cadastro e para a edição (inclusive de registros antigos sem SEI).
/// </summary>
public class AfastamentoSeiObrigatorioTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const int Ano = 2026;
    private const int Mes = 9;
    private const string Login = "chefe.sei";
    private const string MensagemEsperada = "Informe o número do processo SEI do afastamento.";

    private sealed record Contexto(Guid ServidorId, Guid AfastamentoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var setor = b.AdicionarSetor("Núcleo de Balística", "NB");
            var servidor = b.AdicionarServidor(setor, "Servidor Afastado");
            var chefe = b.AdicionarServidor(setor, "Chefe do Setor");
            b.AdicionarChefia(setor, chefe, TipoChefia.ChefiaImediata);
            b.AdicionarUsuario(chefe, Login, CatalogSeed.PerfilChefeSetorId);

            var afastamento = b.AdicionarAfastamento(
                servidor, new DateOnly(Ano, Mes, 1), new DateOnly(Ano, Mes, 5), "FR", sei: "SEI-123");

            return new Contexto(servidor.Id, afastamento.Id);
        });

    private async Task<T> ExecutarAsync<T>(Func<AfastamentoService, Task<T>> acao)
    {
        await using var db = NewContext();
        return await acao(new AfastamentoService(db));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cadastrar_sem_sei_e_recusado(string? sei)
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.CreateAsync(
            new CreateAfastamentoRequest(
                ctx.ServidorId, new DateOnly(Ano, Mes, 10), new DateOnly(Ano, Mes, 12), "FR", null, sei),
            Login));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(MensagemEsperada);
    }

    [Fact]
    public async Task Cadastrar_com_sei_funciona()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.CreateAsync(
            new CreateAfastamentoRequest(
                ctx.ServidorId, new DateOnly(Ano, Mes, 10), new DateOnly(Ano, Mes, 12), "FR", null, "SEI-456"),
            Login));

        result.Error.ShouldBeNull();
        result.Value!.Sei.ShouldBe("SEI-456");
    }

    [Fact]
    public async Task Editar_apagando_o_sei_e_recusado()
    {
        var ctx = await PrepararAsync();

        var result = await ExecutarAsync(s => s.UpdateAsync(
            ctx.AfastamentoId,
            new UpdateAfastamentoRequest(
                new DateOnly(Ano, Mes, 1), new DateOnly(Ano, Mes, 5), "FR", null, null),
            Login));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(MensagemEsperada);
    }
}
