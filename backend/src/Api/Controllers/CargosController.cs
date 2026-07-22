using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cargos")]
public class CargosController(ICargoService cargoService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.CargosListar)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await cargoService.ListAsync(cancellationToken));
}
