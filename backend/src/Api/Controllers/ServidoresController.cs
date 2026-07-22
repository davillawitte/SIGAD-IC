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

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.ServidoresListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await servidorService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.ServidoresCriar)]
    public async Task<IActionResult> Create([FromBody] CreateServidorRequest request, CancellationToken cancellationToken)
    {
        var result = await servidorService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.ServidoresEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServidorRequest request, CancellationToken cancellationToken)
    {
        var result = await servidorService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
