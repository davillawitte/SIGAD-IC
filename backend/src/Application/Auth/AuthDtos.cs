using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Auth;

public record LoginRequest(string Login, string Senha);

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UsuarioAuthDto Usuario);

public record PerfilAuthDto(
    string Codigo,
    IReadOnlyList<string> Permissoes,
    IReadOnlyDictionary<string, Abrangencia> AbrangenciaPorPermissao);

/// <summary>Resumo (id + sigla + tipo) de um setor ou núcleo que o usuário chefia — usado só para
/// exibição (ex.: "Chefe do X" no menu; no setor Direção IC, "Diretor(a)"/"Subcoordenador(a)"
/// conforme `TipoChefia`), não entra em nenhuma checagem de autorização. Núcleos não têm o
/// conceito de `TipoChefia` (chefia única) — sempre vêm com `ChefiaImediata`.</summary>
public record ChefiaResumoDto(Guid Id, string Sigla, TipoChefia TipoChefia);

public record UsuarioAuthDto(
    Guid Id,
    string Login,
    string Nome,
    string? Email,
    IReadOnlyList<string> Perfis,
    IReadOnlyList<string> Permissoes,
    Guid ServidorId,
    Guid? SetorLotacaoId,
    string? SetorLotacaoNome,
    Guid? NucleoLotacaoId,
    string? NucleoLotacaoNome,
    IReadOnlyList<Guid> SetoresGerenciadosIds,
    IReadOnlyList<Guid> NucleosGerenciadosIds,
    IReadOnlyList<Guid> SetoresDosNucleosGerenciadosIds,
    bool DeveAlterarSenha,
    IReadOnlyList<PerfilAuthDto> PerfisDetalhe,
    IReadOnlyList<ChefiaResumoDto> SetoresGeridos,
    IReadOnlyList<ChefiaResumoDto> NucleosGeridos);

public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
