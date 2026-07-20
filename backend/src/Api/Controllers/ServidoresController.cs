using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Servidores;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/servidores")]
public class ServidoresController(IServidorService servidorService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.ServidoresListar)]
    public async Task<IActionResult> List([FromQuery] bool? semUsuario, CancellationToken cancellationToken) =>
        Ok(await servidorService.ListAsync(semUsuario, cancellationToken));

    [HttpPost]
    [RequiresPermission(PermissionCodes.ServidoresCriar)]
    public async Task<IActionResult> Create([FromBody] CreateServidorRequest request, CancellationToken cancellationToken)
    {
        var result = await servidorService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
