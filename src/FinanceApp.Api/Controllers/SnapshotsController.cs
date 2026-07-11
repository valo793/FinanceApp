using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Snapshots;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/snapshots")]
public sealed class SnapshotsController(FinanceDbContext dbContext, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? accountId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BalanceSnapshots.Where(x => x.UserId == currentUser.UserId);

        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.SnapshotDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.SnapshotDate <= to.Value);
        }

        var results = await query
            .OrderBy(x => x.SnapshotDate)
            .Select(x => new BalanceSnapshotDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Balance = x.Balance,
                SnapshotDate = x.SnapshotDate
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }
}
