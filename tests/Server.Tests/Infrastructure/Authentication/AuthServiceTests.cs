using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;

namespace Server.Tests.Infrastructure.Authentication;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_DelegatesToAdapter()
    {
        var adapter = new Mock<IAuthAdapter>();
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var expected = new LoginResult("user", "token");

        adapter.Setup(a => a.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AuthService(adapter.Object, logger);

        var result = await service.LoginAsync(request, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task LoginAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var logger = new Mock<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");

        adapter.Setup(a => a.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var service = new AuthService(adapter.Object, logger.Object);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
