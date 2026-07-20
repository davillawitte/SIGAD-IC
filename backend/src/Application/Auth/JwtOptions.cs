namespace TemplateSistema.Application.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "sigad-ic";
    public string Audience { get; set; } = "sigad-ic-frontend";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
