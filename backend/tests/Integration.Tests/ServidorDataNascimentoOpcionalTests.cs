using Microsoft.EntityFrameworkCore;
using Shouldly;
using TemplateSistema.Application.Servidores;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Infrastructure.Services;
using TemplateSistema.Integration.Tests.Infra;

namespace TemplateSistema.Integration.Tests;

/// <summary>
/// Data de nascimento do servidor passou a ser opcional (era obrigatória da tela até o
/// <c>NOT NULL</c> no banco). Continua valendo a recusa de data futura, mas só quando há valor.
/// </summary>
public class ServidorDataNascimentoOpcionalTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    // Cadastrar/editar servidor é da Gestão Institucional (servidores.criar / servidores.editar),
    // não da chefia do setor — por isso o ator aqui tem o perfil de Direção do IC.
    private const string DirecaoIc = "direcao.ic";

    private sealed record Contexto(Guid SetorId, Guid CargoId);

    private Task<Contexto> PrepararAsync() =>
        SemearAsync(b =>
        {
            var direcao = b.AdicionarDirecaoIc();
            var setor = b.AdicionarSetor("Setor de Papiloscopia", "SP");
            var diretor = b.AdicionarServidor(direcao, "Diretor do IC");
            b.AdicionarChefia(direcao, diretor, TipoChefia.Diretor);
            b.AdicionarUsuario(diretor, DirecaoIc, CatalogSeed.PerfilDirecaoIcId);
            return new Contexto(setor.Id, b.Cargo.Id);
        });

    private static CreateServidorRequest NovoServidor(Contexto ctx, DateOnly? dataNascimento) =>
        new(
            Nome: "Servidor Sem Data",
            Matricula: "777.777-7",
            Cpf: "77777777777",
            CargoId: ctx.CargoId,
            Email: null,
            SetorId: ctx.SetorId,
            NucleoId: null,
            DataNascimento: dataNascimento,
            Telefone: null,
            Status: StatusServidor.Ativo);

    [Fact]
    public async Task Cadastra_servidor_sem_data_de_nascimento()
    {
        var ctx = await PrepararAsync();

        Guid criadoId;
        await using (var db = NewContext())
        {
            var result = await new ServidorService(db).CreateAsync(NovoServidor(ctx, null), DirecaoIc);

            result.Error.ShouldBeNull();
            result.Value!.DataNascimento.ShouldBeNull();
            criadoId = result.Value.Id;
        }

        await using var leitura = NewContext();
        (await leitura.Servidores.AsNoTracking().FirstAsync(x => x.Id == criadoId))
            .DataNascimento.ShouldBeNull();
    }

    [Fact]
    public async Task Recusa_data_de_nascimento_no_futuro()
    {
        var ctx = await PrepararAsync();
        var amanha = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

        await using var db = NewContext();
        var result = await new ServidorService(db).CreateAsync(NovoServidor(ctx, amanha), DirecaoIc);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("Data de nascimento inválida.");
    }

    [Fact]
    public async Task Limpa_a_data_de_nascimento_na_edicao()
    {
        var ctx = await PrepararAsync();

        Guid servidorId;
        await using (var db = NewContext())
        {
            var criado = await new ServidorService(db).CreateAsync(
                NovoServidor(ctx, new DateOnly(1985, 4, 12)), DirecaoIc);
            criado.Error.ShouldBeNull();
            criado.Value!.DataNascimento.ShouldBe(new DateOnly(1985, 4, 12));
            servidorId = criado.Value.Id;
        }

        await using (var db = NewContext())
        {
            var atualizado = await new ServidorService(db).UpdateAsync(
                servidorId,
                new UpdateServidorRequest(
                    Nome: "Servidor Sem Data",
                    Matricula: "777.777-7",
                    Cpf: "77777777777",
                    CargoId: ctx.CargoId,
                    Email: null,
                    SetorId: ctx.SetorId,
                    NucleoId: null,
                    DataNascimento: null,
                    Telefone: null,
                    Status: StatusServidor.Ativo),
                DirecaoIc);

            atualizado.Error.ShouldBeNull();
            atualizado.Value!.DataNascimento.ShouldBeNull();
        }

        await using var leitura = NewContext();
        (await leitura.Servidores.AsNoTracking().FirstAsync(x => x.Id == servidorId))
            .DataNascimento.ShouldBeNull();
    }
}
