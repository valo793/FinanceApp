using System;

namespace FinanceApp.Contracts.UserPreferences;

public sealed class UserPreferenceDto
{
    public string Theme { get; init; } = "dark";
    public string? AccentColor { get; init; }
    public string Density { get; init; } = "comfortable";
    public bool ShowValuesOnStart { get; init; } = true;
    public string DefaultDashboardPeriod { get; init; } = "current_month";
}

public sealed class UpdatePreferenceRequest
{
    public string Theme { get; init; } = "dark";
    public string? AccentColor { get; init; }
    public string Density { get; init; } = "comfortable";
    public bool ShowValuesOnStart { get; init; } = true;
    public string DefaultDashboardPeriod { get; init; } = "current_month";
}
