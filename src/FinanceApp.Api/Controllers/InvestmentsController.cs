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
public sealed class InvestmentsController(IInvestmentService investmentService, ICurrentUserContext currentUser, IAuditService auditService, IAssetPriceService priceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? walletId, [FromQuery] bool? isWatchlist, CancellationToken cancellationToken) =>
        Ok(await investmentService.ListAsync(currentUser.UserId, walletId, isWatchlist, cancellationToken));

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string ticker, CancellationToken cancellationToken)
    {
        var result = await priceService.ValidateTickerAsync(ticker, cancellationToken);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await investmentService.GetAsync(currentUser.UserId, id, cancellationToken));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Ok(await investmentService.GetPortfolioSummaryAsync(currentUser.UserId, cancellationToken));

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] string? category, [FromQuery] Guid? investmentId, CancellationToken cancellationToken) =>
        Ok(await investmentService.GetHistoryAsync(currentUser.UserId, category, investmentId, cancellationToken));

    [HttpGet("{id:guid}/candlesticks")]
    public async Task<IActionResult> GetCandlesticks(Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var periodFrom = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var periodTo = to ?? DateOnly.FromDateTime(DateTime.Today);
        var data = await investmentService.GetInvestmentCandlesticksAsync(currentUser.UserId, id, periodFrom, periodTo, cancellationToken);
        return Ok(data);
    }

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

    [HttpPut("{id:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid id, CancellationToken cancellationToken)
    {
        var result = await investmentService.TogglePinAsync(currentUser.UserId, id, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.pin", "investment", id, "success", "info", new { result.Name, result.IsPinned }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        await investmentService.SyncPricesAsync(currentUser.UserId, cancellationToken);
        await auditService.WriteAsync(currentUser.UserId, "investment.sync", "user", currentUser.UserId, "success", "info", new { }, cancellationToken);
        return Ok();
    }

    [HttpGet("benchmark/{ticker}")]
    public async Task<IActionResult> GetBenchmarkCandlesticks(string ticker, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var periodFrom = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var periodTo = to ?? DateOnly.FromDateTime(DateTime.Today);
        var data = await priceService.GetHistoricalCandlesticksAsync(ticker, periodFrom, periodTo, cancellationToken);
        return Ok(data);
    }
}
