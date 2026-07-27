using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Afastamentos;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/afastamentos")]
public class AfastamentosController(IAfastamentoService afastamentoService) : ControllerBase
{
    /// <summary>Gestão do Setor: só afastamentos dos setores em que o usuário é chefia.</summary>
    [HttpGet("setor")]
    [RequiresPermission(PermissionCodes.AfastamentosListar)]
    public Task<IActionResult> ListSetor(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? servidorId,
        [FromQuery] int? ano,
        [FromQuery] int? mes,
        [FromQuery] string? tipoOcorrenciaCodigo,
        [FromQuery] List<Guid>? servidorIds,
        CancellationToken cancellationToken) =>
        ListInternal("setor", setorId, servidorId, ano, mes, tipoOcorrenciaCodigo, servidorIds, cancellationToken);

    /// <summary>Gestão Institucional: afastamentos de todos os setores, exceto a Direção do IC.</summary>
    [HttpGet("institucionais")]
    [RequiresPermission(PermissionCodes.AfastamentosListar)]
    public Task<IActionResult> ListInstitucionais(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? servidorId,
        [FromQuery] int? ano,
        [FromQuery] int? mes,
        [FromQuery] string? tipoOcorrenciaCodigo,
        [FromQuery] List<Guid>? servidorIds,
        CancellationToken cancellationToken) =>
        ListInternal(
            "institucional",
            setorId,
            servidorId,
            ano,
            mes,
            tipoOcorrenciaCodigo,
            servidorIds,
            cancellationToken);

    [HttpGet]
    [RequiresPermission(PermissionCodes.AfastamentosListar)]
    public Task<IActionResult> List(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? servidorId,
        [FromQuery] int? ano,
        [FromQuery] int? mes,
        [FromQuery] string? tipoOcorrenciaCodigo,
        [FromQuery] List<Guid>? servidorIds,
        [FromQuery] string? escopo = null,
        CancellationToken cancellationToken = default) =>
        ListInternal(escopo, setorId, servidorId, ano, mes, tipoOcorrenciaCodigo, servidorIds, cancellationToken);

    private async Task<IActionResult> ListInternal(
        string? escopo,
        Guid? setorId,
        Guid? servidorId,
        int? ano,
        int? mes,
        string? tipoOcorrenciaCodigo,
        List<Guid>? servidorIds,
        CancellationToken cancellationToken)
    {
        var result = await afastamentoService.ListAsync(
            new AfastamentoListQuery
            {
                SetorId = setorId,
                ServidorId = servidorId,
                Ano = ano,
                Mes = mes,
                TipoOcorrenciaCodigo = tipoOcorrenciaCodigo,
                ServidorIds = servidorIds,
                Escopo = escopo,
            },
            User.GetLogin(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.AfastamentosListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await afastamentoService.GetByIdAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.AfastamentosCriar)]
    public async Task<IActionResult> Create([FromBody] CreateAfastamentoRequest request, CancellationToken cancellationToken)
    {
        var result = await afastamentoService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.AfastamentosEditar)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAfastamentoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await afastamentoService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.AfastamentosExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await afastamentoService.DeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
