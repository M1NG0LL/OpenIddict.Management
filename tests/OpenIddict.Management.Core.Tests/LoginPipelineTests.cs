using FluentAssertions;
using OpenIddict.Management.Enums;
using OpenIddict.Management.Models;
using Xunit;

namespace OpenIddict.Management.Core.Tests;

public class LoginPipelineTests
{
    [Fact]
    public void LoginResult_Success_SetsExpectedProperties()
    {
        var result = LoginResult.Success("usr-456", "john.doe");

        result.Succeeded.Should().BeTrue();
        result.Type.Should().Be(LoginResultType.Success);
        result.UserId.Should().Be("usr-456");
        result.Username.Should().Be("john.doe");
        result.RequiresTwoFactor.Should().BeFalse();
        result.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void LoginResult_InvalidCredentials_SetsExpectedProperties()
    {
        var result = LoginResult.InvalidCredentials("Bad password");

        result.Succeeded.Should().BeFalse();
        result.Type.Should().Be(LoginResultType.InvalidCredentials);
        result.ErrorHeader.Should().Be("InvalidCredentials");
        result.ErrorMessage.Should().Be("Bad password");
    }

    [Fact]
    public void LoginResult_LockedOut_SetsExpectedProperties()
    {
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        var result = LoginResult.LockedOut(lockoutEnd, "Account temporarily locked.");

        result.Succeeded.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
        result.Type.Should().Be(LoginResultType.LockedOut);
        result.LockoutEnd.Should().Be(lockoutEnd);
    }

    [Fact]
    public void LoginResult_TwoFactorRequired_SetsExpectedProperties()
    {
        var result = LoginResult.TwoFactorRequired("usr-789", "jane.doe");

        result.Succeeded.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeTrue();
        result.Type.Should().Be(LoginResultType.RequiresTwoFactor);
        result.UserId.Should().Be("usr-789");
    }

    [Fact]
    public void LoginResult_Failed_SetsExpectedProperties()
    {
        var result = LoginResult.Failed("CustomError", "Something went wrong");

        result.Succeeded.Should().BeFalse();
        result.Type.Should().Be(LoginResultType.Failed);
        result.ErrorHeader.Should().Be("CustomError");
        result.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void LoginContext_PropertiesCanBeSetAndRetrieved()
    {
        var context = new LoginContext
        {
            Username = "test.user",
            Password = "password123",
            RememberMe = true,
            TwoFactorCode = "123456",
            TwoFactorRecoveryCode = "rec-code",
            ClientApplicationId = "client-app",
            IPAddress = "127.0.0.1",
            UserAgent = "Mozilla/5.0"
        };

        context.Username.Should().Be("test.user");
        context.Password.Should().Be("password123");
        context.RememberMe.Should().BeTrue();
        context.TwoFactorCode.Should().Be("123456");
        context.TwoFactorRecoveryCode.Should().Be("rec-code");
        context.ClientApplicationId.Should().Be("client-app");
        context.IPAddress.Should().Be("127.0.0.1");
        context.UserAgent.Should().Be("Mozilla/5.0");
    }
}
