using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Application.Auth;

/// <summary>
/// Contexto do ator autenticado. A avaliação de acesso a recurso é <b>por perfil</b>:
/// a permissão e a abrangência daquela permissão precisam estar no mesmo perfil.
/// SuperAdmin não bypassa operação — só traz permissões de Administração do Sistema
/// no próprio perfil; o restante vem de outros perfis do usuário.
/// </summary>
public sealed class ActorContext
{
    public Guid UsuarioId { get; }
    public Guid ServidorId { get; }
    public string Login { get; }
    public bool IsSuperAdmin { get; }
    public IReadOnlyList<Guid> SetoresGerenciadosIds { get; }
    public IReadOnlyList<PerfilConcessao> Perfis { get; }

    private readonly HashSet<Guid> _setoresGerenciados;

    public ActorContext(
        Guid usuarioId,
        Guid servidorId,
        string login,
        bool isSuperAdmin,
        IReadOnlyList<Guid> setoresGerenciadosIds,
        IReadOnlyList<PerfilConcessao> perfis)
    {
        UsuarioId = usuarioId;
        ServidorId = servidorId;
        Login = login;
        IsSuperAdmin = isSuperAdmin;
        SetoresGerenciadosIds = setoresGerenciadosIds;
        Perfis = perfis;
        _setoresGerenciados = setoresGerenciadosIds.ToHashSet();
    }

    public static ActorContext Empty { get; } = new(
        Guid.Empty,
        Guid.Empty,
        string.Empty,
        false,
        [],
        []);

    /// <summary>Gate grosso: união das permissões dos perfis (sem bypass).</summary>
    public bool TemPermissao(string code) =>
        Perfis.Any(p => p.Permissoes.Contains(code));

    /// <summary>
    /// Avaliação por perfil: permissão + abrangência <b>daquela permissão</b> cobrindo o setor.
    /// </summary>
    public bool PodeAcessar(string permissionCode, Guid? setorId = null) =>
        Perfis.Any(p =>
            p.Permissoes.Contains(permissionCode) && Abrange(p, permissionCode, setorId));

    public bool Abrange(PerfilConcessao p, string permissionCode, Guid? setorId)
    {
        if (p.AbrangenciaDe(permissionCode) == Abrangencia.TodosOsSetores)
        {
            return true;
        }

        return setorId is null || _setoresGerenciados.Contains(setorId.Value);
    }

    /// <summary>
    /// Visão global do módulo: perfil com <c>*.listar</c> em
    /// <see cref="Abrangencia.TodosOsSetores"/> naquele módulo.
    /// </summary>
    public bool TemVisaoGlobal(string modulo)
    {
        var normalizado = modulo.Trim().ToLowerInvariant();
        var listar = $"{normalizado}.listar";
        return Perfis.Any(p =>
            p.Permissoes.Contains(listar)
            && p.AbrangenciaDe(listar) == Abrangencia.TodosOsSetores);
    }

    public IReadOnlyList<Guid>? SetoresVisiveis(string modulo) =>
        TemVisaoGlobal(modulo) ? null : SetoresGerenciadosIds;

    public static string ModuloDe(string permissionCode) => PermissionCodes.ModuloDe(permissionCode);

    public bool PodeVer(string listarPermission, Guid setorId)
    {
        var modulo = ModuloDe(listarPermission);
        if (TemVisaoGlobal(modulo) && TemPermissao(listarPermission))
        {
            return true;
        }

        return PodeAcessar(listarPermission, setorId);
    }
}

public sealed class PerfilConcessao
{
    public Guid PerfilId { get; }
    public string Codigo { get; }
    public HashSet<string> Permissoes { get; }
    public IReadOnlyDictionary<string, Abrangencia> AbrangenciaPorPermissao { get; }

    public PerfilConcessao(
        Guid perfilId,
        string codigo,
        IEnumerable<(string Codigo, Abrangencia Abrangencia)> permissoes)
    {
        PerfilId = perfilId;
        Codigo = codigo;
        var list = permissoes.ToList();
        Permissoes = new HashSet<string>(list.Select(x => x.Codigo), StringComparer.OrdinalIgnoreCase);
        AbrangenciaPorPermissao = list.ToDictionary(
            x => x.Codigo,
            x => x.Abrangencia,
            StringComparer.OrdinalIgnoreCase);
    }

    public Abrangencia AbrangenciaDe(string permissionCode) =>
        AbrangenciaPorPermissao.TryGetValue(permissionCode, out var value)
            ? value
            : Abrangencia.MeusSetores;
}
