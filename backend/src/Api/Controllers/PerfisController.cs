using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Perfis;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/perfis")]
public class PerfisController(IPerfilService perfilService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.PerfisListar)]
    public async Task<IActionResult> List([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken) =>
        Ok(await perfilService.ListAsync(pagination, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.PerfisListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await perfilService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.PerfisCriar)]
    public async Task<IActionResult> Create([FromBody] CreatePerfilRequest request, CancellationToken cancellationToken)
    {
        var result = await perfilService.CreateAsync(request, User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.PerfisEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePerfilRequest request, CancellationToken cancellationToken)
    {
        var result = await perfilService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.PerfisExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await perfilService.SoftDeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/permissoes")]
    [RequiresPermission(PermissionCodes.PerfisGerenciarPermissoes)]
    public async Task<IActionResult> SetPermissoes(
        Guid id,
        [FromBody] SetPerfilPermissoesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await perfilService.SetPermissoesAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
