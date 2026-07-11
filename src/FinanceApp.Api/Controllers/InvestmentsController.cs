using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Investments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/investments")]
public sealed class InvestmentsController(IInvestmentService investmentService, ICurrentUserContext currentUser, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? walletId, CancellationToken cancellationToken) =>
        Ok(await investmentService.ListAsync(currentUser.UserId, walletId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await investmentService.GetAsync(currentUser.UserId, id, cancellationToken));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Ok(await investmentService.GetPortfolioSummaryAsync(currentUser.UserId, cancellationToken));

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken) =>
        Ok(await investmentService.GetHistoryAsync(currentUser.UserId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var investment = await investmentService.CreateAsync(currentUser.UserId, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.create", "investment", investment.Id, "success", "info", new { investment.Name, investment.Ticker }, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = investment.Id }, investment);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvestmentRequest request, CancellationToken cancellationToken)
    {
        var result = await investmentService.UpdateAsync(currentUser.UserId, id, request, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.update", "investment", id, "success", "info", new { request.Name }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await investmentService.DeleteAsync(currentUser.UserId, id, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.delete", "investment", id, "success", "info", new { }, cancellationToken);
        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        await investmentService.SyncPricesAsync(currentUser.UserId, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.sync", "user", currentUser.UserId, "success", "info", new { }, cancellationToken);
        return Ok();
    }
}
