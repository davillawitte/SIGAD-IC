using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permissoes;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/permissoes")]
public class PermissoesController(IPermissaoService permissaoService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.PermissoesListar)]
    public async Task<IActionResult> List([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken) =>
        Ok(await permissaoService.ListAsync(pagination, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.PermissoesListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await permissaoService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.PermissoesCriar)]
    public async Task<IActionResult> Create([FromBody] CreatePermissaoRequest request, CancellationToken cancellationToken)
    {
        var result = await permissaoService.CreateAsync(request, User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.PermissoesEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissaoRequest request, CancellationToken cancellationToken)
    {
        var result = await permissaoService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.PermissoesExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await permissaoService.SoftDeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
