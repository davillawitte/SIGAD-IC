namespace TemplateSistema.Application.Common;

public static class PermissionAreas
{
    public const string GestaoInstitucional = "Gestão Institucional";
    public const string GestaoDoSetor = "Gestão do Setor";
    public const string AdministracaoDoSistema = "Administração do Sistema";

    public static readonly IReadOnlyList<string> Operacionais =
    [
        GestaoDoSetor,
        GestaoInstitucional,
    ];
}

/// <summary>
/// Expande áreas de acesso em permissões concretas. Regra do produto:
/// quem tem a área, tem tudo nela. Escopo de listagem:
/// Gestão do Setor → setores da chefia (ex.: só Direção IC);
/// Gestão Institucional → todos os setores exceto a Direção IC (ver + devolver escalas);
/// em servidores/estrutura institucional o CRUD é completo (TodosOsSetores).
/// </summary>
public static class PermissionAreaGrants
{
    /// <summary>
    /// Escalas/afastamentos liberados pela Gestão Institucional em visão global
    /// (listar/exportar/devolver — sem mutação operacional de outros setores).
    /// </summary>
    private static readonly string[] VisaoInstitucionalExtras =
    [
        PermissionCodes.EscalasListar,
        PermissionCodes.EscalasExportar,
        PermissionCodes.EscalasDevolver,
        PermissionCodes.AfastamentosListar,
    ];

    public static IReadOnlyDictionary<string, Domain.Enums.Abrangencia> Expand(IEnumerable<string> areas)
    {
        var selected = areas
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var map = new Dictionary<string, Domain.Enums.Abrangencia>(StringComparer.OrdinalIgnoreCase);

        if (selected.Contains(PermissionAreas.GestaoDoSetor))
        {
            foreach (var (codigo, _, _, area, _) in PermissionCodes.Catalog)
            {
                if (string.Equals(area, PermissionAreas.GestaoDoSetor, StringComparison.Ordinal))
                {
                    map[codigo] = Domain.Enums.Abrangencia.MeusSetores;
                }
            }
        }

        if (selected.Contains(PermissionAreas.GestaoInstitucional))
        {
            foreach (var (codigo, _, _, area, _) in PermissionCodes.Catalog)
            {
                if (string.Equals(area, PermissionAreas.GestaoInstitucional, StringComparison.Ordinal))
                {
                    map[codigo] = Domain.Enums.Abrangencia.TodosOsSetores;
                }
            }

            foreach (var codigo in VisaoInstitucionalExtras)
            {
                map[codigo] = Domain.Enums.Abrangencia.TodosOsSetores;
            }
        }

        if (selected.Contains(PermissionAreas.AdministracaoDoSistema))
        {
            foreach (var (codigo, _, _, area, _) in PermissionCodes.Catalog)
            {
                if (string.Equals(area, PermissionAreas.AdministracaoDoSistema, StringComparison.Ordinal))
                {
                    map[codigo] = Domain.Enums.Abrangencia.MeusSetores;
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Detecta quais áreas um conjunto de permissões cobre (≥70% dos códigos da área,
    /// ou visão institucional completa para Gestão Institucional).
    /// </summary>
    public static IReadOnlyList<string> DetectAreas(
        IEnumerable<string> permissionCodes,
        IReadOnlyDictionary<string, Domain.Enums.Abrangencia>? abrangenciaPorCodigo = null)
    {
        var codes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var areas = new List<string>();

        var setorCodes = PermissionCodes.Catalog
            .Where(x => x.Area == PermissionAreas.GestaoDoSetor)
            .Select(x => x.Codigo)
            .ToList();
        if (setorCodes.Count > 0 && setorCodes.Count(c => codes.Contains(c)) >= Math.Ceiling(setorCodes.Count * 0.7))
        {
            areas.Add(PermissionAreas.GestaoDoSetor);
        }

        var instCodes = PermissionCodes.Catalog
            .Where(x => x.Area == PermissionAreas.GestaoInstitucional)
            .Select(x => x.Codigo)
            .ToList();
        var hasInstCore = instCodes.Count > 0
            && instCodes.Count(c => codes.Contains(c)) >= Math.Ceiling(instCodes.Count * 0.7);
        var hasVisaoGlobalEscalas = codes.Contains(PermissionCodes.EscalasListar)
            && abrangenciaPorCodigo is not null
            && abrangenciaPorCodigo.TryGetValue(PermissionCodes.EscalasListar, out var abr)
            && abr == Domain.Enums.Abrangencia.TodosOsSetores;
        if (hasInstCore || hasVisaoGlobalEscalas)
        {
            areas.Add(PermissionAreas.GestaoInstitucional);
        }

        var adminCodes = PermissionCodes.Catalog
            .Where(x => x.Area == PermissionAreas.AdministracaoDoSistema)
            .Select(x => x.Codigo)
            .ToList();
        if (adminCodes.Count > 0 && adminCodes.All(c => codes.Contains(c)))
        {
            areas.Add(PermissionAreas.AdministracaoDoSistema);
        }

        return areas;
    }
}


/// <summary>
/// Módulos do catálogo de permissões. A abrangência de um perfil é definida por módulo.
/// </summary>
public static class PermissionModules
{
    public const string Usuarios = "usuarios";
    public const string Perfis = "perfis";
    public const string Permissoes = "permissoes";
    public const string Nucleos = "nucleos";
    public const string Setores = "setores";
    public const string Cargos = "cargos";
    public const string Servidores = "servidores";
    public const string Escalas = "escalas";
    public const string Afastamentos = "afastamentos";

    /// <summary>Módulos cuja abrangência não faz sentido: são globais por natureza.</summary>
    public static readonly IReadOnlySet<string> SemAbrangencia =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Usuarios,
            Perfis,
            Permissoes,
            Cargos,
        };

    public static readonly IReadOnlyList<string> All =
    [
        Escalas,
        Afastamentos,
        Servidores,
        Setores,
        Nucleos,
        Cargos,
        Usuarios,
        Perfis,
        Permissoes,
    ];
}

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
    public const string ServidoresExcluir = "servidores.excluir";

    public const string EscalasListar = "escalas.listar";
    public const string EscalasCriar = "escalas.criar";
    public const string EscalasEditar = "escalas.editar";
    public const string EscalasPublicar = "escalas.publicar";
    public const string EscalasExportar = "escalas.exportar";
    public const string EscalasFinalizar = "escalas.finalizar";
    public const string EscalasExcluir = "escalas.excluir";
    public const string EscalasSolicitarDevolucao = "escalas.solicitar_devolucao";
    public const string EscalasDevolver = "escalas.devolver";

    public const string AfastamentosListar = "afastamentos.listar";
    public const string AfastamentosCriar = "afastamentos.criar";
    public const string AfastamentosEditar = "afastamentos.editar";
    public const string AfastamentosExcluir = "afastamentos.excluir";

    public static readonly IReadOnlyList<(string Codigo, string Nome, string Modulo, string Area, string Descricao)> Catalog =
    [
        (UsuariosListar, "Listar usuários", "usuarios", PermissionAreas.AdministracaoDoSistema, "Visualizar usuários do sistema"),
        (UsuariosCriar, "Criar usuários", "usuarios", PermissionAreas.AdministracaoDoSistema, "Cadastrar novos usuários"),
        (UsuariosEditar, "Editar usuários", "usuarios", PermissionAreas.AdministracaoDoSistema, "Alterar dados e perfis de usuários"),
        (UsuariosBloquear, "Bloquear usuários", "usuarios", PermissionAreas.AdministracaoDoSistema, "Bloquear ou desbloquear acesso"),
        (PerfisListar, "Listar perfis", "perfis", PermissionAreas.AdministracaoDoSistema, "Visualizar perfis de acesso"),
        (PerfisCriar, "Criar perfis", "perfis", PermissionAreas.AdministracaoDoSistema, "Cadastrar novos perfis"),
        (PerfisEditar, "Editar perfis", "perfis", PermissionAreas.AdministracaoDoSistema, "Alterar dados de perfis"),
        (PerfisExcluir, "Excluir perfis", "perfis", PermissionAreas.AdministracaoDoSistema, "Desativar ou remover perfis"),
        (PerfisGerenciarPermissoes, "Gerenciar permissões do perfil", "perfis", PermissionAreas.AdministracaoDoSistema, "Associar ou remover permissões de um perfil"),
        (PermissoesListar, "Listar permissões", "permissoes", PermissionAreas.AdministracaoDoSistema, "Visualizar catálogo de permissões do sistema"),
        (NucleosListar, "Listar núcleos", "nucleos", PermissionAreas.GestaoInstitucional, "Visualizar núcleos da estrutura organizacional"),
        (NucleosCriar, "Criar núcleos", "nucleos", PermissionAreas.GestaoInstitucional, "Cadastrar núcleos"),
        (NucleosEditar, "Editar núcleos", "nucleos", PermissionAreas.GestaoInstitucional, "Alterar núcleos"),
        (NucleosExcluir, "Excluir núcleos", "nucleos", PermissionAreas.GestaoInstitucional, "Remover núcleos sem setores vinculados"),
        (SetoresListar, "Listar setores", "setores", PermissionAreas.GestaoInstitucional, "Visualizar setores"),
        (SetoresCriar, "Criar setores", "setores", PermissionAreas.GestaoInstitucional, "Cadastrar setores"),
        (SetoresEditar, "Editar setores", "setores", PermissionAreas.GestaoInstitucional, "Alterar setores"),
        (SetoresExcluir, "Excluir setores", "setores", PermissionAreas.GestaoInstitucional, "Remover setores sem servidores lotados"),
        (CargosListar, "Listar cargos", "cargos", PermissionAreas.GestaoInstitucional, "Visualizar cargos oficiais"),
        (ServidoresListar, "Listar servidores", "servidores", PermissionAreas.GestaoInstitucional, "Visualizar servidores"),
        (ServidoresCriar, "Criar servidores", "servidores", PermissionAreas.GestaoInstitucional, "Cadastrar servidores"),
        (ServidoresEditar, "Editar servidores", "servidores", PermissionAreas.GestaoInstitucional, "Alterar servidores"),
        (ServidoresExcluir, "Excluir servidores", "servidores", PermissionAreas.GestaoInstitucional, "Excluir servidores sem vínculos bloqueantes"),
        (EscalasListar, "Listar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Visualizar escalas dos setores autorizados"),
        (EscalasCriar, "Criar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Cadastrar e copiar escalas"),
        (EscalasEditar, "Editar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Alterar rascunhos e escalas finalizadas"),
        (EscalasFinalizar, "Finalizar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Finalizar montagem da escala"),
        (EscalasPublicar, "Publicar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Publicar escalas finalizadas"),
        (EscalasExcluir, "Excluir escalas", "escalas", PermissionAreas.GestaoDoSetor, "Excluir escalas em rascunho ou finalizadas"),
        (EscalasSolicitarDevolucao, "Solicitar devolução de escala", "escalas", PermissionAreas.GestaoDoSetor, "Solicitar devolução de escala publicada"),
        (EscalasDevolver, "Aprovar devolução de escala", "escalas", PermissionAreas.GestaoInstitucional, "Aprovar ou recusar devolução de escalas"),
        (EscalasExportar, "Exportar escalas", "escalas", PermissionAreas.GestaoDoSetor, "Exportar escalas em PDF"),
        (AfastamentosListar, "Listar afastamentos", "afastamentos", PermissionAreas.GestaoDoSetor, "Visualizar afastamentos dos servidores"),
        (AfastamentosCriar, "Criar afastamentos", "afastamentos", PermissionAreas.GestaoDoSetor, "Cadastrar afastamentos"),
        (AfastamentosEditar, "Editar afastamentos", "afastamentos", PermissionAreas.GestaoDoSetor, "Alterar afastamentos"),
        (AfastamentosExcluir, "Excluir afastamentos", "afastamentos", PermissionAreas.GestaoDoSetor, "Remover afastamentos"),
    ];

    private static readonly IReadOnlyDictionary<string, string> ModulePorCodigo =
        Catalog.ToDictionary(x => x.Codigo, x => x.Modulo, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> AreaPorCodigo =
        Catalog.ToDictionary(x => x.Codigo, x => x.Area, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Módulo de uma permissão. Cai no prefixo antes do ponto quando o código não está
    /// no catálogo (permissão criada fora dele).
    /// </summary>
    public static string ModuloDe(string codigo)
    {
        if (ModulePorCodigo.TryGetValue(codigo, out var modulo))
        {
            return modulo;
        }

        var separador = codigo.IndexOf('.');
        return separador > 0 ? codigo[..separador].ToLowerInvariant() : codigo.ToLowerInvariant();
    }

    public static bool IsAdministracaoDoSistema(string codigo) =>
        AreaPorCodigo.TryGetValue(codigo, out var area)
        && string.Equals(area, PermissionAreas.AdministracaoDoSistema, StringComparison.Ordinal);
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
    public const string PeritoCriminal = "PC";
    public const string AgenteTecnicoForense = "ATF";
    public const string AgenteNecropsia = "AN";
    public const string AssistenteTecnicoForense = "ASTF";
    public const string Estagiario = "EST";
    public const string Terceirizado = "TER";
    public const string ServidorExterno = "EXT";

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

    /// <summary>Códigos longos legados da seed antiga → sigla atual.</summary>
    public static readonly IReadOnlyDictionary<string, string> ObsoleteToSigla =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PERITO_CRIMINAL"] = PeritoCriminal,
            ["AGENTE_TECNICO_FORENSE"] = AgenteTecnicoForense,
            ["AGENTE_NECROPSIA"] = AgenteNecropsia,
            ["ASSISTENTE_TECNICO_FORENSE"] = AssistenteTecnicoForense,
            ["ESTAGIARIO"] = Estagiario,
            ["TERCEIRIZADO"] = Terceirizado,
            ["SERVIDOR_EXTERNO"] = ServidorExterno,
        };
}

public static class PerfilCodes
{
    public const string SuperAdministrador = "SUPERADMINISTRADOR";
    public const string ChefeSetor = "CHEFE_SETOR";
    public const string Servidor = "SERVIDOR";
    public const string DirecaoIc = "DIRECAO_IC";
}
