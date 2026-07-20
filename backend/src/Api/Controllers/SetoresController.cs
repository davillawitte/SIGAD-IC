using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/setores")]
public class SetoresController(ISetorService setorService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.SetoresListar)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await setorService.ListAsync(cancellationToken));
}
