using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/padroes-escala")]
public class PadroesEscalaController(IEscalaService escalaService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> List(
        [FromQuery] TipoFuncionamento? tipoFuncionamento,
        CancellationToken cancellationToken)
    {
        var items = await escalaService.ListPadroesAsync(tipoFuncionamento, cancellationToken);
        return Ok(items);
    }
}
