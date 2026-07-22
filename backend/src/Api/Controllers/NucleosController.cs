using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Nucleos;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/nucleos")]
public class NucleosController(INucleoService nucleoService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.NucleosListar)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await nucleoService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.NucleosListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await nucleoService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.NucleosCriar)]
    public async Task<IActionResult> Create([FromBody] CreateNucleoRequest request, CancellationToken cancellationToken)
    {
        var result = await nucleoService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.NucleosEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNucleoRequest request, CancellationToken cancellationToken)
    {
        var result = await nucleoService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.NucleosExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await nucleoService.DeleteAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
