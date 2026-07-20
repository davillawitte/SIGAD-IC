using System.Security.Claims;

namespace TemplateSistema.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetLogin(this ClaimsPrincipal user) =>
        user.FindFirstValue("login")
        ?? user.FindFirstValue(ClaimTypes.Name)
        ?? user.Identity?.Name
        ?? "system";
}
