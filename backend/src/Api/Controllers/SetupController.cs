using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Setup;

namespace TemplateSistema.Api.Controllers;

/// <summary>
/// Wizard de provisionamento do superadministrador (token de uso único). Anônimo por natureza —
/// a proteção é o token + o autodesligamento: enquanto existir superadministrador ativo,
/// qualquer ação aqui (inclusive <c>status</c>) responde 410 Gone.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("api/setup")]
public class SetupController(
    ISetupService setupService,
    IValidator<CompleteSetupRequest> completeValidator) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var status = await setupService.GetStatusAsync(cancellationToken);
        return status.NeedsSetup ? Ok(status) : StatusCode(StatusCodes.Status410Gone);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(
        [FromBody] CompleteSetupRequest request,
        CancellationToken cancellationToken)
    {
        var status = await setupService.GetStatusAsync(cancellationToken);
        if (!status.NeedsSetup)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        var validation = await completeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await setupService.CompleteAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
