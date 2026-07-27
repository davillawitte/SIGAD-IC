using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateToken(UsuarioAuthDto usuario)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var isSuperAdmin = usuario.Perfis.Any(p =>
            string.Equals(p, PerfilCodes.SuperAdministrador, StringComparison.OrdinalIgnoreCase));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Login),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new("login", usuario.Login),
            new("is_super_admin", isSuperAdmin ? "true" : "false"),
        };

        claims.Add(new Claim("servidorId", usuario.ServidorId.ToString()));
        if (usuario.SetorLotacaoId is Guid setorLotacao)
        {
            claims.Add(new Claim("setorLotacaoId", setorLotacao.ToString()));
        }

        claims.AddRange(usuario.SetoresGerenciadosIds.Select(id => new Claim("setorGerenciadoId", id.ToString())));
        claims.AddRange(usuario.Perfis.Select(perfil => new Claim(ClaimTypes.Role, perfil)));
        claims.AddRange(usuario.Permissoes.Select(permissao => new Claim("permission", permissao)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
