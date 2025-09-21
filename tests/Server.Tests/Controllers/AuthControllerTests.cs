using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Controllers;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;
using Server.Infrastructure.Database;

namespace Server.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_WhenSuccessful_CommitsTransaction()
    {
        var service = new Mock<IAuthService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<AuthController>>();
        var request = new LoginRequest("user", "password");
        var expected = new LoginResult("user", "token");
        service.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var controller = new AuthController(service.Object, database.Object, logger);

        var result = await controller.Login(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Once);
        database.Verify(d => d.RollbackTran(), Times.Never);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Login_WhenDomainException_RollsBackAndReturnsUnauthorized()
    {
        var service = new Mock<IAuthService>();
        var database = new Mock<IDatabaseContext>();
        var logger = Mock.Of<ILogger<AuthController>>();
        var request = new LoginRequest("user", "password");
        service.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("failed"));

        var controller = new AuthController(service.Object, database.Object, logger);

        var result = await controller.Login(request, CancellationToken.None);

        database.Verify(d => d.BeginTran(), Times.Once);
        database.Verify(d => d.CommitTran(), Times.Never);
        database.Verify(d => d.RollbackTran(), Times.Once);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
