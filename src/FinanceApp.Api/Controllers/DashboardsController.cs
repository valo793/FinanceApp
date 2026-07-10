using FinanceApp.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboards")]
public sealed class DashboardsController(IDashboardService dashboardService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var periodFrom = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var periodTo = to ?? periodFrom.AddMonths(1).AddDays(-1);

        return Ok(await dashboardService.GetOverviewAsync(currentUser.UserId, periodFrom, periodTo, cancellationToken));
    }
}
