namespace FinanceApp.Domain.Entities;

public sealed class UserPreference
{
    public Guid UserId { get; private set; }
    public string Theme { get; private set; } = "dark";
    public string? AccentColor { get; private set; }
    public string Density { get; private set; } = "comfortable";
    public Guid? DefaultAccountId { get; private set; }
    public Guid? DefaultWalletId { get; private set; }
    public string DefaultDashboardPeriod { get; private set; } = "current_month";
    public bool ShowValuesOnStart { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private UserPreference() { }

    public UserPreference(Guid userId)
    {
        UserId = userId;
    }

    public void Update(string theme, string? accentColor, string density, bool showValuesOnStart, string defaultDashboardPeriod)
    {
        Theme = theme;
        AccentColor = accentColor;
        Density = density;
        ShowValuesOnStart = showValuesOnStart;
        DefaultDashboardPeriod = defaultDashboardPeriod;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
