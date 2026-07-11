using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projections")]
public sealed class ProjectionsController(IProjectionService projectionService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var result = await projectionService.GetProjectionAsync(currentUser.UserId, months, cancellationToken);
        return Ok(result);
    }
}
