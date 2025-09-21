using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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
        var userManager = CreateUserManagerMock();
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var expected = new LoginResult("user", "token", "CUST001");
        var user = new ApplicationUser
        {
            UserName = request.Username,
            SageCustomerCode = "CUST001"
        };

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync(user);
        userManager.Setup(m => m.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        adapter.Setup(a => a.LoginAsync(It.Is<LoginRequest>(r => r.SageCustomerCode == user.SageCustomerCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AuthService(adapter.Object, userManager.Object, logger);

        var result = await service.LoginAsync(request, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task LoginAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var logger = new Mock<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var user = new ApplicationUser
        {
            UserName = request.Username,
            SageCustomerCode = "CUST001"
        };

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync(user);
        userManager.Setup(m => m.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        adapter.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var service = new AuthService(adapter.Object, userManager.Object, logger.Object);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var logger = new Mock<ILogger<AuthService>>();
        var request = new LoginRequest("missing", "password");

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync((ApplicationUser?)null);

        var service = new AuthService(adapter.Object, userManager.Object, logger.Object);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        adapter.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
