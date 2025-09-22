using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Sage;

namespace Server.Tests.Infrastructure.Authentication;

public class SageAuthAdapterTests
{
    [Fact]
    public async Task LoginAsync_WhenSageAuthenticates_ReturnsLoginResult()
    {
        var sessionManager = new Mock<ISageSessionManager>();
        var logger = new Mock<ILogger<SageAuthAdapter>>();
        var request = new LoginRequest("user", "password");
        var session = new SageSession("token-123");

        sessionManager
            .Setup(manager => manager.AuthenticateAsync(request.Username, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var adapter = new SageAuthAdapter(sessionManager.Object, logger.Object);

        var result = await adapter.LoginAsync(request, CancellationToken.None);

        result.Should().Be(new LoginResult(request.Username, session.Token));
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsInvalid_ThrowsDomainException()
    {
        var sessionManager = new Mock<ISageSessionManager>();
        var logger = new Mock<ILogger<SageAuthAdapter>>();
        var request = new LoginRequest("user", "bad-password");

        sessionManager
            .Setup(manager => manager.AuthenticateAsync(request.Username, request.Password, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SageAuthenticationException("invalid credentials"));

        var adapter = new SageAuthAdapter(sessionManager.Object, logger.Object);

        var act = async () => await adapter.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid Sage username or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenSageFails_ThrowsDomainException()
    {
        var sessionManager = new Mock<ISageSessionManager>();
        var logger = new Mock<ILogger<SageAuthAdapter>>();
        var request = new LoginRequest("user", "password");

        sessionManager
            .Setup(manager => manager.AuthenticateAsync(request.Username, request.Password, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var adapter = new SageAuthAdapter(sessionManager.Object, logger.Object);

        var act = async () => await adapter.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Unable to authenticate with Sage.");
    }
}
