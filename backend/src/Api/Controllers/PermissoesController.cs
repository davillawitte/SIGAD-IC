using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;

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
}
