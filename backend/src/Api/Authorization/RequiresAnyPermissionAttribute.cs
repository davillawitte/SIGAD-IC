using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TemplateSistema.Api.Authorization;

/// <summary>Exige pelo menos uma das permissões informadas (OR).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresAnyPermissionAttribute(params string[] permissions) : Attribute, IAuthorizationFilter
{
    public IReadOnlyList<string> Permissions { get; } = permissions;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var hasAny = Permissions.Any(permission =>
            user.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase)));

        if (!hasAny)
        {
            context.Result = new ForbidResult();
        }
    }
}
