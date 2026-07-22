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

    public const string SetoresListar = "setores.listar";
    public const string SetoresCriar = "setores.criar";
    public const string SetoresEditar = "setores.editar";
    public const string SetoresExcluir = "setores.excluir";

    public const string NucleosListar = "nucleos.listar";
    public const string NucleosCriar = "nucleos.criar";
    public const string NucleosEditar = "nucleos.editar";
    public const string NucleosExcluir = "nucleos.excluir";

    public const string CargosListar = "cargos.listar";

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
        (PermissoesListar, "Listar permissões", "permissoes", "Visualizar catálogo de permissões do sistema"),
        (NucleosListar, "Listar núcleos", "nucleos", "Visualizar núcleos da estrutura organizacional"),
        (NucleosCriar, "Criar núcleos", "nucleos", "Cadastrar núcleos"),
        (NucleosEditar, "Editar núcleos", "nucleos", "Alterar núcleos"),
        (NucleosExcluir, "Excluir núcleos", "nucleos", "Remover núcleos sem setores vinculados"),
        (SetoresListar, "Listar setores", "setores", "Visualizar setores"),
        (SetoresCriar, "Criar setores", "setores", "Cadastrar setores"),
        (SetoresEditar, "Editar setores", "setores", "Alterar setores"),
        (SetoresExcluir, "Excluir setores", "setores", "Remover setores sem servidores lotados"),
        (CargosListar, "Listar cargos", "cargos", "Visualizar cargos oficiais"),
        (ServidoresListar, "Listar servidores", "servidores", "Visualizar servidores"),
        (ServidoresCriar, "Criar servidores", "servidores", "Cadastrar servidores"),
        (ServidoresEditar, "Editar servidores", "servidores", "Alterar servidores"),
    ];
}

public static class SetorSiglas
{
    public const string DirecaoIc = "Direção IC";
    public const string DirecaoIcNome = "Direção do Instituto de Criminalística";

    public static bool IsDirecaoIc(string? sigla)
    {
        if (string.IsNullOrWhiteSpace(sigla))
        {
            return false;
        }

        return string.Equals(
            Normalize(sigla),
            Normalize(DirecaoIc),
            StringComparison.Ordinal);
    }

    public static string Normalize(string value) =>
        value.Trim()
            .ToLowerInvariant()
            .Replace('á', 'a')
            .Replace('à', 'a')
            .Replace('â', 'a')
            .Replace('ã', 'a')
            .Replace('é', 'e')
            .Replace('ê', 'e')
            .Replace('í', 'i')
            .Replace('ó', 'o')
            .Replace('ô', 'o')
            .Replace('õ', 'o')
            .Replace('ú', 'u')
            .Replace('ç', 'c');
}

public static class CargoCodes
{
    public const string PeritoCriminal = "PERITO_CRIMINAL";
    public const string AgenteTecnicoForense = "AGENTE_TECNICO_FORENSE";
    public const string AgenteNecropsia = "AGENTE_NECROPSIA";
    public const string AssistenteTecnicoForense = "ASSISTENTE_TECNICO_FORENSE";
    public const string Estagiario = "ESTAGIARIO";
    public const string Terceirizado = "TERCEIRIZADO";
    public const string ServidorExterno = "SERVIDOR_EXTERNO";

    public static readonly IReadOnlyList<(string Codigo, string Nome)> Catalog =
    [
        (PeritoCriminal, "Perito Criminal"),
        (AgenteTecnicoForense, "Agente Técnico Forense"),
        (AgenteNecropsia, "Agente de Necrópsia"),
        (AssistenteTecnicoForense, "Assistente Técnico Forense"),
        (Estagiario, "Estagiário"),
        (Terceirizado, "Terceirizado"),
        (ServidorExterno, "Servidor Externo"),
    ];
}

public static class PerfilCodes
{
    public const string SuperAdministrador = "SUPERADMINISTRADOR";
    public const string ChefeSetor = "CHEFE_SETOR";
    public const string Servidor = "SERVIDOR";
}
