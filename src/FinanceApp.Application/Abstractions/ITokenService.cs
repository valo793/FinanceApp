using FinanceApp.Contracts.Auth;

namespace FinanceApp.Application.Abstractions;

public interface ITokenService
{
    LoginResponse IssueTokens(Guid userId, string email, string fullName, string theme, bool mfaEnabled = false);
    string IssueChallengeToken(Guid userId, string email);
    System.Security.Claims.ClaimsPrincipal? ValidateChallengeToken(string token);
}
