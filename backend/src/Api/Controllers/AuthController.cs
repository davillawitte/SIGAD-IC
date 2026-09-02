using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Auth;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<LoginRequest> loginValidator,
    IValidator<AlterarSenhaRequest> alterarSenhaValidator) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await authService.RefreshSessionAsync(User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("alterar-senha")]
    [Authorize]
    public async Task<IActionResult> AlterarSenha(
        [FromBody] AlterarSenhaRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await alterarSenhaValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await authService.AlterarSenhaAsync(User.GetLogin(), request, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
