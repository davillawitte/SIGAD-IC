using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateSistema.Api.Authorization;
using TemplateSistema.Application.Abstractions;
using TemplateSistema.Application.Common;

namespace TemplateSistema.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tipos-ocorrencia")]
public class TiposOcorrenciaController(IEscalaService escalaService) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(PermissionCodes.EscalasListar)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await escalaService.ListTiposOcorrenciaAsync(cancellationToken));
}
