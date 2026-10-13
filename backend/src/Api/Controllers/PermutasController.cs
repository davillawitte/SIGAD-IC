using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Permutas;

namespace TemplateSistema.Api.Controllers;

/// <summary>Permutas da Gestão do Setor. Sem código de permissão próprio: permuta altera escala,
/// então usa as permissões de escala; a chefia da lotação é validada no service.</summary>
[ApiController]
[Authorize]
[Route("api/permutas")]
public class PermutasController(IPermutaService permutaService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> List(
        [FromQuery] int? ano,
        [FromQuery] int? mes,
        [FromQuery] Guid? servidorId,
        CancellationToken cancellationToken) =>
        Ok(await permutaService.ListAsync(new PermutaListQuery(ano, mes, servidorId), User.GetLogin(), cancellationToken));

    [HttpGet("servidores")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> ListServidores(CancellationToken cancellationToken) =>
        Ok(await permutaService.ListServidoresAsync(User.GetLogin(), cancellationToken));

    [HttpGet("escala-servidor")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> GetEscalaServidor(
        [FromQuery] Guid servidorId,
        [FromQuery] int ano,
        [FromQuery] int mes,
        CancellationToken cancellationToken)
    {
        if (ano is < 2000 or > 2100 || mes is < 1 or > 12)
        {
            return BadRequest(new { message = "Mês/ano inválido." });
        }

        return Ok(await permutaService.GetEscalaServidorAsync(servidorId, ano, mes, cancellationToken));
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Create([FromBody] CreatePermutaRequest request, CancellationToken cancellationToken)
    {
        var result = await permutaService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await permutaService.DeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
