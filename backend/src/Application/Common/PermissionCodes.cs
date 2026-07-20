namespace TemplateSistema.Application.Common;

public static class PermissionCodes
{
    public const string UsuariosListar = "usuarios.listar";
    public const string UsuariosCriar = "usuarios.criar";
    public const string UsuariosEditar = "usuarios.editar";
    public const string UsuariosBloquear = "usuarios.bloquear";

    public const string PerfisListar = "perfis.listar";
    public const string PerfisCriar = "perfis.criar";
    public const string PerfisEditar = "perfis.editar";
    public const string PerfisExcluir = "perfis.excluir";
    public const string PerfisGerenciarPermissoes = "perfis.gerenciar_permissoes";

    public const string PermissoesListar = "permissoes.listar";
    public const string PermissoesCriar = "permissoes.criar";
    public const string PermissoesEditar = "permissoes.editar";
    public const string PermissoesExcluir = "permissoes.excluir";

    public const string SetoresListar = "setores.listar";
    public const string SetoresCriar = "setores.criar";
    public const string SetoresEditar = "setores.editar";

    public const string ServidoresListar = "servidores.listar";
    public const string ServidoresCriar = "servidores.criar";
    public const string ServidoresEditar = "servidores.editar";

    public static readonly IReadOnlyList<(string Codigo, string Nome, string Modulo, string Descricao)> Catalog =
    [
        (UsuariosListar, "Listar usuários", "usuarios", "Visualizar usuários do sistema"),
        (UsuariosCriar, "Criar usuários", "usuarios", "Cadastrar novos usuários"),
        (UsuariosEditar, "Editar usuários", "usuarios", "Alterar dados e perfis de usuários"),
        (UsuariosBloquear, "Bloquear usuários", "usuarios", "Bloquear ou desbloquear acesso"),
        (PerfisListar, "Listar perfis", "perfis", "Visualizar perfis de acesso"),
        (PerfisCriar, "Criar perfis", "perfis", "Cadastrar novos perfis"),
        (PerfisEditar, "Editar perfis", "perfis", "Alterar dados de perfis"),
        (PerfisExcluir, "Excluir perfis", "perfis", "Desativar ou remover perfis"),
        (PerfisGerenciarPermissoes, "Gerenciar permissões do perfil", "perfis", "Associar ou remover permissões de um perfil"),
        (PermissoesListar, "Listar permissões", "permissoes", "Visualizar catálogo de permissões"),
        (PermissoesCriar, "Criar permissões", "permissoes", "Cadastrar novas permissões"),
        (PermissoesEditar, "Editar permissões", "permissoes", "Alterar permissões"),
        (PermissoesExcluir, "Excluir permissões", "permissoes", "Desativar permissões"),
        (SetoresListar, "Listar setores", "setores", "Visualizar setores"),
        (SetoresCriar, "Criar setores", "setores", "Cadastrar setores"),
        (SetoresEditar, "Editar setores", "setores", "Alterar setores"),
        (ServidoresListar, "Listar servidores", "servidores", "Visualizar servidores"),
        (ServidoresCriar, "Criar servidores", "servidores", "Cadastrar servidores"),
        (ServidoresEditar, "Editar servidores", "servidores", "Alterar servidores"),
    ];
}

public static class PerfilCodes
{
    public const string SuperAdministrador = "SUPERADMINISTRADOR";
    public const string ChefeSetor = "CHEFE_SETOR";
    public const string Servidor = "SERVIDOR";
}
