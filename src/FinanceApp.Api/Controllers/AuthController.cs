using FinanceApp.Application.Abstractions;
using FinanceApp.Contracts.Auth;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FinanceApp.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    FinanceDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAuditService auditService,
    ICurrentUserContext currentUser,
    IValidator<LoginRequest> validator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var exists = await dbContext.Users.AnyAsync(x => x.Email == request.Email.Trim().ToLower(), cancellationToken);
        if (exists)
            return Conflict(new ProblemDetails { Title = "E-mail já cadastrado.", Status = 409 });

        var user = new User(request.Email, passwordHasher.Hash(request.Password));
        var profile = new UserProfile(user.Id, request.Email.Split('@')[0]);
        var preference = new UserPreference(user.Id);
        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(profile);
        dbContext.UserPreferences.Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed default checking account
        var account = new Account(user.Id, "Corretora XP", "checking", "BRL", 50000.00m, true, true);
        dbContext.Accounts.Add(account);

        // Seed default wallet
        var wallet = new Wallet(user.Id, "Principal", "investment", "BRL");
        dbContext.Wallets.Add(wallet);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed default income and expense categories
        var catSalario = new IncomeCategory(user.Id, "Salário", "#22C55E", "briefcase", true);
        var catDividendos = new IncomeCategory(user.Id, "Dividendos", "#3B82F6", "trending-up", true);
        dbContext.IncomeCategories.AddRange(catSalario, catDividendos);

        var catAlimentacao = new ExpenseCategory(user.Id, "Alimentação", "#EF4444", "shopping-cart", true);
        var catTransporte = new ExpenseCategory(user.Id, "Transporte", "#F59E0B", "car", true);
        dbContext.ExpenseCategories.AddRange(catAlimentacao, catTransporte);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed real investments
        var invPetr = new Investment(user.Id, wallet.Id, "Petrobras PN", "PETR4", "stock", 100m, 35.00m, 38.72m, "BRL", "moderate", null, null, null, false);
        var invItub = new Investment(user.Id, wallet.Id, "Itaú Unibanco", "ITUB4", "stock", 200m, 30.00m, 34.18m, "BRL", "moderate", null, null, null, false);
        var invBtc = new Investment(user.Id, wallet.Id, "Bitcoin", "BTC-BRL", "crypto", 0.05m, 350000.00m, 402150.00m, "BRL", "high", null, null, null, false);
        
        // Seed watchlist investments
        var invVale = new Investment(user.Id, wallet.Id, "Vale ON", "VALE3", "stock", 0m, 0m, 61.05m, "BRL", "moderate", null, null, null, true);
        var invAapl = new Investment(user.Id, wallet.Id, "Apple Inc.", "AAPL", "stock", 0m, 0m, 228.60m, "BRL", "low", null, null, null, true);
        
        dbContext.Investments.AddRange(invPetr, invItub, invBtc, invVale, invAapl);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed buy transactions
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tx1 = new Transaction(user.Id, "investment_buy", "Compra PETR4", "confirmed", today.AddDays(-15), null, 3500.00m, "BRL", account.Id, null, null, null, null, null, false, null, "manual", invPetr.Id, 100m, 35.00m);
        var tx2 = new Transaction(user.Id, "investment_buy", "Compra ITUB4", "confirmed", today.AddDays(-10), null, 6000.00m, "BRL", account.Id, null, null, null, null, null, false, null, "manual", invItub.Id, 200m, 30.00m);
        var tx3 = new Transaction(user.Id, "investment_buy", "Compra BTC", "confirmed", today.AddDays(-5), null, 17500.00m, "BRL", account.Id, null, null, null, null, null, false, null, "manual", invBtc.Id, 0.05m, 350000.00m);
        
        // Seed some simple income/expenses for testing
        var txIncome = new Transaction(user.Id, "income", "Salário Mensal", "confirmed", today.AddDays(-30), null, 8000.00m, "BRL", account.Id, null, catSalario.Id, null);
        var txExpense = new Transaction(user.Id, "expense", "Supermercado", "confirmed", today.AddDays(-2), null, 450.00m, "BRL", account.Id, null, null, catAlimentacao.Id);
        
        dbContext.Transactions.AddRange(tx1, tx2, tx3, txIncome, txExpense);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Adjust account balance (Initial R$ 50000 + R$ 8000 (salary) - R$ 27000 (buys) - R$ 450 (expense) = R$ 30550)
        account.RecalculateBalance(-3500m - 6000m - 17500m + 8000m - 450m);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Seed some BalanceSnapshots and InvestmentSnapshots for line chart history
        for (int i = 30; i >= 0; i -= 5)
        {
            var date = today.AddDays(-i);
            var balanceSnapshot = new BalanceSnapshot(user.Id, account.Id, 30000.00m + (30 - i) * 100m, date);
            dbContext.BalanceSnapshots.Add(balanceSnapshot);
            
            var snapPetr = new InvestmentSnapshot(user.Id, invPetr.Id, 3500m + (30 - i) * 30m, date);
            var snapItub = new InvestmentSnapshot(user.Id, invItub.Id, 6000m + (30 - i) * 40m, date);
            var snapBtc = new InvestmentSnapshot(user.Id, invBtc.Id, 17500m + (30 - i) * 130m, date);
            dbContext.InvestmentSnapshots.AddRange(snapPetr, snapItub, snapBtc);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(user.Id, "auth.register", "user", user.Id, "success", "info", new { request.Email }, cancellationToken);

        var tokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark", user.MfaEnabled);

        // Create session for refresh token
        var refreshTokenHash = HashToken(tokens.RefreshToken);
        var session = new Session(user.Id, refreshTokenHash, DateTimeOffset.UtcNow.AddDays(7), request.DeviceName);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        // Check lockout before validating password
        if (user is not null && user.IsLockedOut)
        {
            await auditService.WriteAsync(user.Id, "auth.login.locked", "user", user.Id, "denied", "warning", new { email }, cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "Conta bloqueada por excesso de tentativas. Tente novamente mais tarde.", Status = 401 });
        }

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            if (user is not null)
            {
                user.RegisterLoginFailure();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await auditService.WriteAsync(user?.Id, "auth.login.failed", "user", user?.Id, "failed", "warning", new { email }, cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "Credenciais inválidas.", Status = 401 });
        }

        user.RegisterLoginSuccess();
        var profile = await dbContext.UserProfiles.FirstAsync(x => x.UserId == user.Id, cancellationToken);

        if (user.MfaEnabled)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(user.Id, "auth.login.mfa_pending", "user", user.Id, "success", "info", new { email }, cancellationToken);

            var challengeToken = tokenService.IssueChallengeToken(user.Id, user.Email);
            
            return Ok(new LoginResponse
            {
                AccessToken = challengeToken,
                RefreshToken = string.Empty,
                ExpiresIn = 300,
                RequiresMfa = true,
                User = new CurrentUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = profile.FullName,
                    Theme = "dark"
                }
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.login.success", "user", user.Id, "success", "info", new { email }, cancellationToken);

        var tokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark", user.MfaEnabled);

        // Create session
        var refreshTokenHash = HashToken(tokens.RefreshToken);
        var session = new Session(user.Id, refreshTokenHash, DateTimeOffset.UtcNow.AddDays(7), request.DeviceName);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpPost("login/mfa")]
    public async Task<IActionResult> LoginMfa([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var principal = tokenService.ValidateChallengeToken(request.ChallengeToken);
        if (principal is null)
            return Unauthorized(new ProblemDetails { Title = "Token de desafio inválido ou expirado.", Status = 401 });

        var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                        ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || user.MfaSecret is null)
            return Unauthorized();

        bool isMfaValid = TotpHelper.VerifyCode(user.MfaSecret, request.Code, DateTimeOffset.UtcNow);
        bool isBackupCodeUsed = false;

        if (!isMfaValid && !string.IsNullOrEmpty(user.MfaBackupCodesHash))
        {
            var codeHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code.Trim())));
            var hashesList = user.MfaBackupCodesHash.Split(',').ToList();

            if (hashesList.Contains(codeHash))
            {
                hashesList.Remove(codeHash);
                var newBackupCodesHash = hashesList.Count > 0 ? string.Join(",", hashesList) : null;
                user.UpdateBackupCodes(newBackupCodesHash);
                isMfaValid = true;
                isBackupCodeUsed = true;
            }
        }

        if (!isMfaValid)
        {
            await auditService.WriteAsync(user.Id, "auth.login.mfa.failed", "user", user.Id, "failed", "warning", new { }, cancellationToken);
            return Unauthorized(new ProblemDetails { Title = "Código MFA inválido.", Status = 401 });
        }

        if (isBackupCodeUsed)
        {
            await auditService.WriteAsync(user.Id, "auth.login.mfa.backup_used", "user", user.Id, "success", "info", new { }, cancellationToken);
        }

        user.RegisterLoginSuccess();
        var profile = await dbContext.UserProfiles.FirstAsync(x => x.UserId == user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.login.success", "user", user.Id, "success", "info", new { email = user.Email }, cancellationToken);

        var tokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark", user.MfaEnabled);

        var refreshTokenHash = HashToken(tokens.RefreshToken);
        var session = new Session(user.Id, refreshTokenHash, DateTimeOffset.UtcNow.AddDays(7), "Desktop Client");
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tokens);
    }

    [HttpGet("mfa/setup")]
    [Authorize]
    public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == currentUser.UserId, cancellationToken);
        var secret = TotpHelper.GenerateSecretKey();
        var otpauth = TotpHelper.GetQrCodeUri("FinanceApp", user.Email, secret);

        return Ok(new MfaSetupResponse { SecretKey = secret, OtpAuthUri = otpauth });
    }

    [HttpPost("mfa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableMfa([FromBody] MfaEnableRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == currentUser.UserId, cancellationToken);
        
        if (!TotpHelper.VerifyCode(request.SecretKey, request.Code, DateTimeOffset.UtcNow))
        {
            return BadRequest(new ProblemDetails { Title = "Código de verificação inválido.", Status = 400 });
        }

        var backupCodes = Enumerable.Range(0, 8)
            .Select(_ => RandomNumberGenerator.GetInt32(10000000, 99999999).ToString())
            .ToList();

        var hashes = backupCodes.Select(code =>
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(bytes);
        });
        var backupCodesHash = string.Join(",", hashes);

        user.EnableMfa(request.SecretKey, backupCodesHash);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.mfa.enabled", "user", user.Id, "success", "info", new { }, cancellationToken);

        return Ok(new { BackupCodes = backupCodes });
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableMfa([FromBody] MfaEnableRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstAsync(x => x.Id == currentUser.UserId, cancellationToken);
        if (user.MfaSecret is null)
        {
            return BadRequest(new ProblemDetails { Title = "MFA não está ativo.", Status = 400 });
        }

        if (!TotpHelper.VerifyCode(user.MfaSecret, request.Code, DateTimeOffset.UtcNow))
        {
            return BadRequest(new ProblemDetails { Title = "Código de verificação inválido.", Status = 400 });
        }

        user.DisableMfa();
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "auth.mfa.disabled", "user", user.Id, "success", "info", new { }, cancellationToken);

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = HashToken(request.RefreshToken);
        var session = await dbContext.Sessions.FirstOrDefaultAsync(x => x.RefreshTokenHash == hash, cancellationToken);

        if (session is null || !session.IsValid)
            return Unauthorized(new ProblemDetails { Title = "Refresh token inválido ou expirado.", Status = 401 });

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null)
            return Unauthorized(new ProblemDetails { Title = "Usuário não encontrado.", Status = 401 });

        var profile = await dbContext.UserProfiles.FirstAsync(x => x.UserId == user.Id, cancellationToken);

        // Rotate: revoke current session and create a new one
        var newTokens = tokenService.IssueTokens(user.Id, user.Email, profile.FullName, "dark", user.MfaEnabled);
        var newHash = HashToken(newTokens.RefreshToken);
        var newSession = new Session(user.Id, newHash, DateTimeOffset.UtcNow.AddDays(7), session.DeviceName);
        session.MarkReplaced(newSession.Id);
        dbContext.Sessions.Add(newSession);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(newTokens);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        // Revoke all sessions for the current user
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var sessions = await dbContext.Sessions.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var s in sessions) s.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var sessions = await dbContext.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.LastActivityAt)
            .Select(x => new { x.Id, x.DeviceName, x.LastActivityAt, x.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(sessions);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
