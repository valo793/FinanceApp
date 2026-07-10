using Asp.Versioning;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Recurring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{v:apiVersion}/recurring")]
[Authorize]
public sealed class RecurringController(IRecurringService recurringService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await recurringService.ListAsync(currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await recurringService.GetAsync(currentUser.UserId, id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringRequest request, CancellationToken cancellationToken)
    {
        var result = await recurringService.CreateAsync(currentUser.UserId, request, cancellationToken);
        return Created($"api/v1/recurring/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecurringRequest request, CancellationToken cancellationToken)
    {
        var result = await recurringService.UpdateAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken cancellationToken)
    {
        await recurringService.PauseAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        await recurringService.ResumeAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}
