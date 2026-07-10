using FinanceApp.Application.Abstractions;

namespace FinanceApp.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today(string timezone = "America/Sao_Paulo")
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        var local = TimeZoneInfo.ConvertTime(UtcNow, tz);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
