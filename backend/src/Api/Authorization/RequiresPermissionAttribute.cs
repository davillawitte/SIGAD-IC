using Microsoft.AspNetCore.Authorization;

namespace TemplateSistema.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class AnyPermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}

/// <summary>
/// Exige a permissão informada via claim. Sem bypass de SuperAdmin:
/// o perfil SuperAdmin só carrega permissões de Administração do Sistema.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresPermissionAttribute(string permission)
    : AuthorizeAttribute, IAuthorizationRequirementData
{
    public string Permission { get; } = permission;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(Permission);
    }
}

/// <summary>Exige pelo menos uma das permissões informadas (OR).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAnyPermissionAttribute(params string[] permissions)
    : AuthorizeAttribute, IAuthorizationRequirementData
{
    public IReadOnlyList<string> Permissions { get; } = permissions;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new AnyPermissionRequirement(Permissions);
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    internal static bool HasPermission(System.Security.Claims.ClaimsPrincipal user, string permission) =>
        user.Claims.Any(c =>
            c.Type == "permission"
            && string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
}

public sealed class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        if (requirement.Permissions.Any(p => PermissionAuthorizationHandler.HasPermission(context.User, p)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement opcional para checagens futuras via <c>IAuthorizationService</c>.
/// Os serviços preferem <c>ActorContext.PodeAcessar</c> diretamente.
/// </summary>
public sealed class AbrangenciaRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
