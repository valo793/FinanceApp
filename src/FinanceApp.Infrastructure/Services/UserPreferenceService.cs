using System;
using System.Threading;
using System.Threading.Tasks;
using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.UserPreferences;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class UserPreferenceService(FinanceDbContext dbContext) : IUserPreferenceService
{
    public async Task<UserPreferenceDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity is null)
        {
            entity = new UserPreference(userId);
            dbContext.UserPreferences.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Map(entity);
    }

    public async Task<UserPreferenceDto> UpdateAsync(Guid userId, UpdatePreferenceRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (entity is null)
        {
            entity = new UserPreference(userId);
            dbContext.UserPreferences.Add(entity);
        }
        entity.Update(request.Theme, request.AccentColor, request.Density, request.ShowValuesOnStart, request.DefaultDashboardPeriod);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static UserPreferenceDto Map(UserPreference x) => new()
    {
        Theme = x.Theme,
        AccentColor = x.AccentColor,
        Density = x.Density,
        ShowValuesOnStart = x.ShowValuesOnStart,
        DefaultDashboardPeriod = x.DefaultDashboardPeriod
    };
}
