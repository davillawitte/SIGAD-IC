using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Setores;

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

    [HttpGet("meus")]
    [RequiresAnyPermission(PermissionCodes.SetoresListar, PermissionCodes.EscalasCriar, PermissionCodes.EscalasListar)]
    public async Task<IActionResult> ListMeus(CancellationToken cancellationToken) =>
        Ok(await setorService.ListMeusAsync(User.GetLogin(), cancellationToken));

    [HttpGet("estrutura")]
    [RequiresPermission(PermissionCodes.SetoresListar)]
    public async Task<IActionResult> Estrutura(CancellationToken cancellationToken)
    {
        var result = await setorService.GetEstruturaAsync(cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.SetoresListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await setorService.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost("chefias-conflitos")]
    [RequiresAnyPermission(PermissionCodes.SetoresCriar, PermissionCodes.SetoresEditar)]
    public async Task<IActionResult> PreviewChefiasConflitos(
        [FromBody] PreviewChefiasConflitosRequest request,
        CancellationToken cancellationToken) =>
        Ok(await setorService.PreviewChefiasConflitosAsync(request, cancellationToken));

    [HttpPost]
    [RequiresPermission(PermissionCodes.SetoresCriar)]
    public async Task<IActionResult> Create([FromBody] CreateSetorRequest request, CancellationToken cancellationToken)
    {
        var result = await setorService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.SetoresEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSetorRequest request, CancellationToken cancellationToken)
    {
        var result = await setorService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.SetoresExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await setorService.DeleteAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
