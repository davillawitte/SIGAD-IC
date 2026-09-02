using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Entities;
using TemplateSistema.Domain.Enums;
using TemplateSistema.Persistence;

namespace TemplateSistema.Integration.Tests.Infra;

/// <summary>
/// Monta atores e estrutura organizacional de forma explícita, sem depender do
/// <c>AuthSeed</c>. Não chama SaveChanges: quem orquestra decide quando persistir.
/// </summary>
/// <param name="cargo">
/// Cargo do catálogo. Os cargos são criados pela migration <c>AddCargoTable</c> com
/// Ids fixos, então são reaproveitados em vez de recriados — o índice único em
/// <c>Codigo</c> não permitiria duplicar.
/// </param>
public sealed class CenarioBuilder(ApplicationDbContext db, Cargo cargo)
{
    // Compartilhado entre builders: um teste pode semear em mais de uma etapa e
    // matrícula e CPF têm índice único.
    private static int _sequencial;

    public ApplicationDbContext Db => db;
    public Cargo Cargo => cargo;

    public Setor AdicionarSetor(string nome, string sigla)
    {
        var setor = Setor.Create(nome, sigla, nucleoId: null, createdBy: "teste");
        db.Setores.Add(setor);
        return setor;
    }

    /// <summary>Setor que pertence a um núcleo — servidores lotados aqui também são
    /// visíveis pra quem chefia o núcleo (não só pra quem chefia o setor).</summary>
    public Setor AdicionarSetor(string nome, string sigla, Nucleo nucleo)
    {
        var setor = Setor.Create(nome, sigla, nucleo.Id, createdBy: "teste");
        db.Setores.Add(setor);
        return setor;
    }

    /// <summary>
    /// A sigla precisa ser exatamente a esperada por <see cref="SetorSiglas.IsDirecaoIc"/>,
    /// que hoje decide por comparação de string.
    /// </summary>
    public Setor AdicionarDirecaoIc() =>
        AdicionarSetor(SetorSiglas.DirecaoIcNome, SetorSiglas.DirecaoIc);

    public Servidor AdicionarServidor(Setor setor, string nome, StatusServidor status = StatusServidor.Ativo)
    {
        var n = Interlocked.Increment(ref _sequencial);
        var servidor = Servidor.Create(
            nome,
            matricula: $"{n / 1000 % 1000:000}.{n % 1000:000}-0",
            cpf: $"{n:00000000000}",
            cargoId: cargo.Id,
            email: null,
            setorId: setor.Id,
            nucleoId: null,
            dataNascimento: new DateOnly(1990, 1, 1),
            status: status,
            createdBy: "teste");
        db.Servidores.Add(servidor);
        return servidor;
    }

    /// <summary>Servidor lotado direto no núcleo (chefe de núcleo ou sem setor específico).</summary>
    public Servidor AdicionarServidorNoNucleo(Nucleo nucleo, string nome, StatusServidor status = StatusServidor.Ativo)
    {
        var n = Interlocked.Increment(ref _sequencial);
        var servidor = Servidor.Create(
            nome,
            matricula: $"{n / 1000 % 1000:000}.{n % 1000:000}-0",
            cpf: $"{n:00000000000}",
            cargoId: cargo.Id,
            email: null,
            setorId: null,
            nucleoId: nucleo.Id,
            dataNascimento: new DateOnly(1990, 1, 1),
            status: status,
            createdBy: "teste");
        db.Servidores.Add(servidor);
        return servidor;
    }

    public Usuario AdicionarUsuario(Servidor servidor, string login, params Guid[] perfilIds)
    {
        var usuario = Usuario.Create(servidor.Id, login, senhaHash: "hash-de-teste", createdBy: "teste");
        if (perfilIds.Length > 0)
        {
            usuario.DefinirPerfis(perfilIds);
        }

        db.Usuarios.Add(usuario);
        return usuario;
    }

    /// <summary>
    /// SuperAdmin sozinho só tem Administração do Sistema. Passe perfis extras
    /// (ex.: Direção IC, Chefe) para cobrir operação.
    /// </summary>
    public Usuario AdicionarSuperAdmin(
        Servidor servidor,
        string login = "superadmin",
        params Guid[] perfisOperacionais) =>
        AdicionarUsuario(
            servidor,
            login,
            [CatalogSeed.PerfilSuperAdminId, .. perfisOperacionais]);

    public SetorChefia AdicionarChefia(Setor setor, Servidor servidor, TipoChefia tipoChefia)
    {
        var chefia = SetorChefia.Create(setor.Id, servidor.Id, tipoChefia);
        db.SetorChefias.Add(chefia);
        return chefia;
    }

    public Nucleo AdicionarNucleo(string nome, string sigla, Guid? chefeServidorId = null)
    {
        var nucleo = Nucleo.Create(nome, sigla, chefeServidorId, createdBy: "teste");
        db.Nucleos.Add(nucleo);
        return nucleo;
    }

    public Escala AdicionarEscala(
        Setor setor,
        int ano,
        int mes,
        TipoFuncionamento tipoFuncionamento = TipoFuncionamento.Expediente)
    {
        var escala = Escala.Create(setor.Id, null, ano, mes, tipoFuncionamento, observacao: null, createdBy: "teste");
        db.Escalas.Add(escala);
        return escala;
    }

    public Escala AdicionarEscalaDeNucleo(
        Nucleo nucleo,
        int ano,
        int mes,
        TipoFuncionamento tipoFuncionamento = TipoFuncionamento.Expediente)
    {
        var escala = Escala.Create(null, nucleo.Id, ano, mes, tipoFuncionamento, observacao: null, createdBy: "teste");
        db.Escalas.Add(escala);
        return escala;
    }

    public EscalaServidor AdicionarEscalaServidor(Escala escala, Servidor servidor, int ordem = 1)
    {
        var escalaServidor = EscalaServidor.Create(
            escala.Id,
            servidor.Id,
            cargo.Id,
            ordem,
            servidor.Nome,
            servidor.Matricula,
            cargo.Nome,
            cargo.Codigo,
            createdBy: "teste");
        db.EscalaServidores.Add(escalaServidor);
        return escalaServidor;
    }

    public Afastamento AdicionarAfastamento(
        Servidor servidor,
        DateOnly dataInicio,
        DateOnly dataFim,
        string tipoOcorrenciaCodigo = "FR",
        string? sei = null)
    {
        var afastamento = Afastamento.Create(
            servidor.Id,
            dataInicio,
            dataFim,
            tipoOcorrenciaCodigo,
            sei: sei,
            createdBy: "teste");
        db.Afastamentos.Add(afastamento);
        return afastamento;
    }
}
