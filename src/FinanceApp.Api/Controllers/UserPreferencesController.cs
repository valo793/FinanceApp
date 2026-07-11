using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.UserPreferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/preferences")]
public sealed class UserPreferencesController(IUserPreferenceService preferenceService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await preferenceService.GetAsync(currentUser.UserId, cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePreferenceRequest request, CancellationToken cancellationToken) =>
        Ok(await preferenceService.UpdateAsync(currentUser.UserId, request, cancellationToken));
}
