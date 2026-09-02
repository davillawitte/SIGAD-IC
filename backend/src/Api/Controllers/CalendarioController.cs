using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Calendario;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendario")]
public class CalendarioController(ICalendarioService calendarioService) : ControllerBase
{
    [HttpGet("anos")]
    [RequiresPermission(PermissionCodes.CalendarioListar)]
    public async Task<IActionResult> ListarAnos(CancellationToken cancellationToken) =>
        Ok(await calendarioService.ListarAnosAsync(cancellationToken));

    [HttpGet("anos/{ano:int}")]
    [RequiresPermission(PermissionCodes.CalendarioListar)]
    public async Task<IActionResult> ObterAno(int ano, CancellationToken cancellationToken)
    {
        var result = await calendarioService.ObterAnoAsync(ano, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost("anos")]
    [RequiresPermission(PermissionCodes.CalendarioGerar)]
    public async Task<IActionResult> GerarAno([FromBody] GerarAnoRequest request, CancellationToken cancellationToken)
    {
        var result = await calendarioService.GerarAnoAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(ObterAno), new { ano = result.Value!.Ano }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("anos/{id:guid}")]
    [RequiresPermission(PermissionCodes.CalendarioGerar)]
    public async Task<IActionResult> AtualizarAno(
        Guid id,
        [FromBody] AtualizarAnoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await calendarioService.AtualizarAnoAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("anos/{id:guid}/publicar")]
    [RequiresPermission(PermissionCodes.CalendarioPublicar)]
    public async Task<IActionResult> Publicar(Guid id, CancellationToken cancellationToken)
    {
        var result = await calendarioService.PublicarAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("anos/{id:guid}")]
    [RequiresPermission(PermissionCodes.CalendarioExcluir)]
    public async Task<IActionResult> ExcluirAno(Guid id, CancellationToken cancellationToken)
    {
        var result = await calendarioService.ExcluirAnoAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("marcacoes")]
    [RequiresPermission(PermissionCodes.CalendarioGerenciarMarcacoes)]
    public async Task<IActionResult> AdicionarMarcacao(
        [FromBody] CriarMarcacaoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await calendarioService.AdicionarMarcacaoAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("marcacoes/{id:guid}")]
    [RequiresPermission(PermissionCodes.CalendarioGerenciarMarcacoes)]
    public async Task<IActionResult> AtualizarMarcacao(
        Guid id,
        [FromBody] AtualizarMarcacaoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await calendarioService.AtualizarMarcacaoAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("marcacoes/{id:guid}")]
    [RequiresPermission(PermissionCodes.CalendarioGerenciarMarcacoes)]
    public async Task<IActionResult> ExcluirMarcacao(Guid id, CancellationToken cancellationToken)
    {
        var result = await calendarioService.ExcluirMarcacaoAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    /// <summary>
    /// Leitura aberta a qualquer usuário autenticado: consumida por escalas/prazos
    /// independente de perfil, sem exigir a permissão de Gestão Institucional.
    /// </summary>
    [HttpGet("dias-nao-uteis")]
    public async Task<IActionResult> DiasNaoUteis(
        [FromQuery] DateOnly inicio,
        [FromQuery] DateOnly fim,
        CancellationToken cancellationToken)
    {
        if (fim < inicio)
        {
            return BadRequest(new { message = "Data fim deve ser maior ou igual à data início." });
        }

        return Ok(await calendarioService.ConsultarDiasNaoUteisAsync(inicio, fim, cancellationToken));
    }
}
