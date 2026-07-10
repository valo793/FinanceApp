using Asp.Versioning;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{v:apiVersion}/wallets")]
[Authorize]
public sealed class WalletsController(IWalletService walletService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await walletService.ListAsync(currentUser.UserId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWalletRequest request, CancellationToken cancellationToken)
    {
        var result = await walletService.CreateAsync(currentUser.UserId, request, cancellationToken);
        return Created($"api/v1/wallets/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWalletRequest request, CancellationToken cancellationToken)
    {
        var result = await walletService.UpdateAsync(currentUser.UserId, id, request, cancellationToken);
        return Ok(result);
    }
}
