using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.EscalasResumidas;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Api.Controllers;

/// <summary>
/// Escala resumida por núcleo — para chefes de núcleo e servidores lotados diretamente no
/// núcleo, cuja escala cobre todos os setores que o núcleo engloba numa grade só (por vaga
/// de equipe, não por servidor fixo). Controller irmão de <see cref="EscalasController"/>,
/// não anexado nele: formato de recurso e regras de autorização são diferentes o bastante
/// (âmbito por núcleo, não por setor) pra justificar rotas próprias.
/// </summary>
[ApiController]
[Authorize]
[Route("api/escalas-resumidas")]
public class EscalasResumidasController(
    IEscalaResumidaService escalaResumidaService,
    IEscalaResumidaPdfService escalaResumidaPdfService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? nucleoId,
        [FromQuery] Guid? setorId,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] StatusEscala? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await escalaResumidaService.ListAsync(
            new EscalaResumidaListQuery
            {
                NucleoId = nucleoId,
                SetorId = setorId,
                Mes = mes,
                Ano = ano,
                Status = status,
                Page = page,
                PageSize = pageSize,
                Search = search,
            },
            User.GetLogin(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("anterior")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Anterior(
        [FromQuery] Guid? nucleoId,
        [FromQuery] Guid? setorId,
        [FromQuery] int ano,
        [FromQuery] int mes,
        CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.GetAnteriorAsync(
            nucleoId, setorId, ano, mes, User.GetLogin(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("servidores-elegiveis")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> ServidoresElegiveis(
        [FromQuery] Guid? nucleoId, [FromQuery] Guid? setorId, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ListServidoresElegiveisAsync(
            nucleoId, setorId, User.GetLogin(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.GetByIdAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.EscalasCriar)]
    public async Task<IActionResult> Create([FromBody] CreateEscalaResumidaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEscalaResumidaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/setores")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> ConfigurarSetores(Guid id, [FromBody] ConfigurarSetoresRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ConfigurarSetoresAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/equipes")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> ConfigurarEquipe(Guid id, [FromBody] ConfigurarEquipeRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ConfigurarEquipeAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/equipes/{equipeId:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> AtualizarEquipe(
        Guid id, Guid equipeId, [FromBody] AtualizarEquipeRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.AtualizarEquipeAsync(id, equipeId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/equipes/{equipeId:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> RemoverEquipe(Guid id, Guid equipeId, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.RemoverEquipeAsync(id, equipeId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/equipes/{equipeId:guid}/rotacao")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> ConfigurarRotacao(
        Guid id, Guid equipeId, [FromBody] ConfigurarRotacaoRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ConfigurarRotacaoAsync(id, equipeId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/equipes/{equipeId:guid}/dias")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> UpsertDia(
        Guid id, Guid equipeId, [FromBody] UpsertDiaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.UpsertDiaAsync(id, equipeId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/equipes/{equipeId:guid}/dias/{data}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> ReverterDia(Guid id, Guid equipeId, DateOnly data, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ReverterDiaParaRegraAsync(id, equipeId, data, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/copiar")]
    [RequiresPermission(PermissionCodes.EscalasCriar)]
    public async Task<IActionResult> Copiar(Guid id, [FromBody] CopiarEscalaResumidaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.CopiarAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/vincular-escala")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> VincularEscala(
        Guid id, [FromBody] VincularEscalaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.VincularEscalaAsync(id, request.EscalaId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/finalizar")]
    [RequiresPermission(PermissionCodes.EscalasFinalizar)]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.FinalizarAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/reabrir")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Reabrir(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.ReabrirAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaService.DeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/pdf")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaResumidaPdfService.GenerateAsync(id, User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }
}
