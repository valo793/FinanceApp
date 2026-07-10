using FinanceApp.Contracts.Auth;

namespace FinanceApp.Application.Abstractions;

public interface ITokenService
{
    LoginResponse IssueTokens(Guid userId, string email, string fullName, string theme);
}
