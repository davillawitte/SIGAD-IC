using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Api.Extensions;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;
using TemplateSistema.Application.Escalas;
using TemplateSistema.Domain.Enums;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/escalas")]
public class EscalasController(
    IEscalaService escalaService,
    IEscalaPdfService escalaPdfService,
    IEscalaCsvService escalaCsvService) : ControllerBase
{
    /// <summary>Gestão do Setor: só escalas dos setores em que o usuário é chefia.</summary>
    [HttpGet("setor")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public Task<IActionResult> ListSetor(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] StatusEscala? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken cancellationToken = default) =>
        ListInternal("setor", setorId, nucleoId, mes, ano, status, page, pageSize, search, sort, dir, cancellationToken);

    /// <summary>Gestão Institucional: escalas de todos os setores, exceto a Direção do IC.</summary>
    [HttpGet("institucionais")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public Task<IActionResult> ListInstitucionais(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] StatusEscala? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken cancellationToken = default) =>
        ListInternal("institucional", setorId, nucleoId, mes, ano, status, page, pageSize, search, sort, dir, cancellationToken);

    /// <summary>Calendário do mês da Gestão do Setor — ocorrências achatadas (quem trabalha em
    /// cada dia) das escalas dos setores/núcleos em que o usuário é chefia.</summary>
    [HttpGet("setor/calendario")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public Task<IActionResult> CalendarioSetor(
        [FromQuery] int ano,
        [FromQuery] int mes,
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] Guid? servidorId,
        CancellationToken cancellationToken = default) =>
        CalendarioMesInternal("setor", ano, mes, setorId, nucleoId, servidorId, cancellationToken);

    /// <summary>Mesmo calendário, na visão institucional: todos os setores, exceto a Direção do
    /// IC (que é gerida em Gestão do Setor).</summary>
    [HttpGet("institucionais/calendario")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public Task<IActionResult> CalendarioInstitucional(
        [FromQuery] int ano,
        [FromQuery] int mes,
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] Guid? servidorId,
        CancellationToken cancellationToken = default) =>
        CalendarioMesInternal("institucional", ano, mes, setorId, nucleoId, servidorId, cancellationToken);

    private async Task<IActionResult> CalendarioMesInternal(
        string escopo,
        int ano,
        int mes,
        Guid? setorId,
        Guid? nucleoId,
        Guid? servidorId,
        CancellationToken cancellationToken)
    {
        var query = new EscalaCalendarioMesQuery(ano, mes, setorId, nucleoId, servidorId, escopo);
        var result = await escalaService.GetCalendarioMesAsync(query, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpGet]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public Task<IActionResult> List(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] int? mes,
        [FromQuery] int? ano,
        [FromQuery] StatusEscala? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? escopo = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? dir = null,
        CancellationToken cancellationToken = default) =>
        ListInternal(escopo, setorId, nucleoId, mes, ano, status, page, pageSize, search, sort, dir, cancellationToken);

    private async Task<IActionResult> ListInternal(
        string? escopo,
        Guid? setorId,
        Guid? nucleoId,
        int? mes,
        int? ano,
        StatusEscala? status,
        int page,
        int pageSize,
        string? search,
        string? sort,
        string? dir,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.ListAsync(
            new EscalaListQuery
            {
                SetorId = setorId,
                NucleoId = nucleoId,
                Mes = mes,
                Ano = ano,
                Status = status,
                Page = page,
                PageSize = pageSize,
                Search = search,
                Escopo = escopo,
                Sort = sort,
                Dir = dir,
            },
            User.GetLogin(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("devolucoes/pendentes")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> ListDevolucoesPendentes(CancellationToken cancellationToken)
    {
        var result = await escalaService.ListDevolucoesPendentesAsync(User.GetLogin(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("devolucoes/{solicitacaoId:guid}/aprovar")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> AprovarDevolucao(
        Guid solicitacaoId,
        [FromBody] ResponderDevolucaoEscalaRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.AprovarDevolucaoAsync(solicitacaoId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("devolucoes/{solicitacaoId:guid}/recusar")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> RecusarDevolucao(
        Guid solicitacaoId,
        [FromBody] ResponderDevolucaoEscalaRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.RecusarDevolucaoAsync(solicitacaoId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("conflitos-servidores")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> ConflitosServidores(
        [FromBody] CheckConflitosServidoresRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.CheckConflitosServidoresAsync(request, User.GetLogin(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("anterior")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> EscalaAnterior(
        [FromQuery] Guid? setorId,
        [FromQuery] Guid? nucleoId,
        [FromQuery] int ano,
        [FromQuery] int mes,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.GetEscalaAnteriorAsync(setorId, nucleoId, ano, mes, User.GetLogin(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.GetByIdAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { message = result.Error });
    }

    [HttpGet("{id:guid}/calendario")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Calendario(Guid id, [FromQuery] Guid? servidorId, CancellationToken cancellationToken)
    {
        var result = await escalaService.GetCalendarioAsync(id, servidorId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/cobertura")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Cobertura(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.GetCoberturaAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/conflitos")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Conflitos(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.GetConflitosAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost]
    [RequiresPermission(PermissionCodes.EscalasCriar)]
    public async Task<IActionResult> Create([FromBody] CreateEscalaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.CreateAsync(request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEscalaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.UpdateAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/servidores")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> AddServidores(Guid id, [FromBody] AddEscalaServidoresRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.AddServidoresAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/gerar")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Gerar(Guid id, [FromBody] GerarEscalaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.GerarEscalaAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/aplicar-afastamentos")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> AplicarAfastamentos(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.AplicarAfastamentosAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/servidores/{servidorId:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> RemoveServidor(Guid id, Guid servidorId, CancellationToken cancellationToken)
    {
        var result = await escalaService.RemoveServidorAsync(id, servidorId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/servidores/{servidorId:guid}/jornadas")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> AddJornada(
        Guid id,
        Guid servidorId,
        [FromBody] CreateEscalaJornadaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.AddJornadaAsync(id, servidorId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/jornadas/{jornadaId:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> DeleteJornada(Guid id, Guid jornadaId, CancellationToken cancellationToken)
    {
        var result = await escalaService.DeleteJornadaAsync(id, jornadaId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/servidores/{servidorId:guid}/ocorrencias")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> UpsertOcorrencia(
        Guid id,
        Guid servidorId,
        [FromBody] UpsertOcorrenciaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.UpsertOcorrenciaAsync(id, servidorId, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/ocorrencias/lote")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> UpsertOcorrenciasLote(
        Guid id,
        [FromBody] UpsertOcorrenciasLoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.UpsertOcorrenciasLoteAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}/ocorrencias/sync")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> SyncOcorrencias(
        Guid id,
        [FromBody] SyncOcorrenciasRequest request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.SyncOcorrenciasAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/ocorrencias/{ocorrenciaId:guid}")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> DeleteOcorrencia(Guid id, Guid ocorrenciaId, CancellationToken cancellationToken)
    {
        var result = await escalaService.DeleteOcorrenciaAsync(id, ocorrenciaId, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/publicar")]
    [RequiresPermission(PermissionCodes.EscalasPublicar)]
    public async Task<IActionResult> Publicar(
        Guid id,
        [FromBody] PublicarEscalaRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.PublicarAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/finalizar")]
    [RequiresPermission(PermissionCodes.EscalasFinalizar)]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.FinalizarAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/reabrir")]
    [RequiresPermission(PermissionCodes.EscalasEditar)]
    public async Task<IActionResult> Reabrir(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.ReabrirAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(PermissionCodes.EscalasExcluir)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.DeleteAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/solicitar-devolucao")]
    [RequiresPermission(PermissionCodes.EscalasSolicitarDevolucao)]
    public async Task<IActionResult> SolicitarDevolucao(
        Guid id,
        [FromBody] SolicitarDevolucaoEscalaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await escalaService.SolicitarDevolucaoAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/devolver")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Devolver(Guid id, CancellationToken cancellationToken)
    {
        var result = await escalaService.DevolverAsync(id, User.GetLogin(), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/copiar")]
    [RequiresPermission(PermissionCodes.EscalasCriar)]
    public async Task<IActionResult> Copiar(Guid id, [FromBody] CopiarEscalaRequest request, CancellationToken cancellationToken)
    {
        var result = await escalaService.CopiarAsync(id, request, User.GetLogin(), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/pdf")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Pdf(Guid id, [FromQuery] string layout = "horizontal", CancellationToken cancellationToken = default)
    {
        var result = await escalaPdfService.GenerateAsync(id, layout, User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return File(result.Value!.Content, "application/pdf", result.Value.FileName);
    }

    [HttpGet("{id:guid}/csv")]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> Csv(Guid id, [FromQuery] string opcao = "resumida", CancellationToken cancellationToken = default)
    {
        if (!string.Equals(opcao, "resumida", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Formato de exportação CSV ainda não implementado." });
        }

        var result = await escalaCsvService.GenerateResumidoAsync(id, User.GetLogin(), cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return File(
            result.Value!.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.Value.FileName);
    }
}
