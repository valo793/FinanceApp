using System;
using FinanceApp.Infrastructure.Security;
using Xunit;

namespace FinanceApp.UnitTests;

public sealed class TotpHelperTests
{
    [Fact]
    public void GenerateSecretKey_ShouldReturnValidBase32String()
    {
        // Act
        var secret = TotpHelper.GenerateSecretKey();

        // Assert
        Assert.NotNull(secret);
        Assert.NotEmpty(secret);
        // Base32 secret should contain only characters A-Z and 2-7
        Assert.Matches("^[A-Z2-7]+$", secret);
    }

    [Fact]
    public void GenerateCode_ShouldReturnSixDigitString()
    {
        // Arrange
        var secret = TotpHelper.GenerateSecretKey();
        var time = DateTimeOffset.UtcNow;

        // Act
        var code = TotpHelper.GenerateCode(secret, time);

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.Matches("^[0-9]{6}$", code);
    }

    [Fact]
    public void VerifyCode_ShouldValidateCorrectCodeAndRejectIncorrect()
    {
        // Arrange
        var secret = TotpHelper.GenerateSecretKey();
        var time = DateTimeOffset.UtcNow;
        var correctCode = TotpHelper.GenerateCode(secret, time);

        // Act & Assert
        Assert.True(TotpHelper.VerifyCode(secret, correctCode, time));
        Assert.False(TotpHelper.VerifyCode(secret, "123456", time));
    }

    [Fact]
    public void VerifyCode_ShouldHandleTimeDriftWithinWindow()
    {
        // Arrange
        var secret = TotpHelper.GenerateSecretKey();
        var time = DateTimeOffset.UtcNow;
        
        // Code generated 25 seconds ago (could be in the previous 30s counter step)
        var pastTime = time.AddSeconds(-25);
        var pastCode = TotpHelper.GenerateCode(secret, pastTime);

        // Act & Assert
        // Verified with current time, which should pass if drift window is >= 1
        Assert.True(TotpHelper.VerifyCode(secret, pastCode, time, driftWindow: 1));
    }
}
