namespace FinanceApp.Contracts.Auth;

public sealed class MfaSetupResponse
{
    public required string SecretKey { get; init; }
    public required string OtpAuthUri { get; init; }
}

public sealed class MfaEnableRequest
{
    public required string SecretKey { get; init; }
    public required string Code { get; init; }
}

public sealed class MfaVerifyRequest
{
    public required string ChallengeToken { get; init; }
    public required string Code { get; init; }
}
