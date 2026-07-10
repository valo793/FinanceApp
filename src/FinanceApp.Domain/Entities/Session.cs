namespace FinanceApp.Domain.Entities;

public sealed class Session
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public Guid RefreshTokenFamilyId { get; private set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public string? IpHash { get; private set; }
    public string? DeviceName { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; } = DateTimeOffset.UtcNow;

    private Session() { }

    public Session(Guid userId, string refreshTokenHash, DateTimeOffset expiresAt, string? deviceName = null, string? ipHash = null)
    {
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        ExpiresAt = expiresAt;
        DeviceName = deviceName;
        IpHash = ipHash;
    }

    public bool IsValid => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReplaced(Guid newSessionId)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedBySessionId = newSessionId;
    }

    public void RefreshActivity()
    {
        LastActivityAt = DateTimeOffset.UtcNow;
    }
}
