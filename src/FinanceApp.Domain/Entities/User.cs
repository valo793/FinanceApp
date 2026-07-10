using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordHashVersion { get; private set; } = "argon2id";
    public string Status { get; private set; } = "active";
    public bool EmailVerified { get; private set; }
    public bool MfaEnabled { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockoutUntil { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public bool IsLockedOut => LockoutUntil.HasValue && LockoutUntil.Value > DateTimeOffset.UtcNow;

    private User() { }

    public User(string email, string passwordHash)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    public void RegisterLoginSuccess()
    {
        FailedLoginCount = 0;
        LockoutUntil = null;
        LastLoginAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RegisterLoginFailure(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        }

        Touch();
    }
}
