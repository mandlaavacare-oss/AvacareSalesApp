using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;

namespace Server.Tests.Infrastructure.Authentication;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_PassesCustomerCodeToAdapter()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var user = new ApplicationUser { UserName = request.Username, SageCustomerCode = "CUST-001" };
        var expected = new LoginResult(request.Username, "token", "CUST-001");

        userManager.Setup(m => m.FindByNameAsync(request.Username)).ReturnsAsync(user);
        signInManager.Setup(s => s.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);
        adapter.Setup(a => a.LoginAsync(request, user.SageCustomerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        var result = await service.LoginAsync(request, CancellationToken.None);

        result.Should().Be(expected);
        adapter.Verify(a => a.LoginAsync(request, user.SageCustomerCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenAdapterFails_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = new Mock<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var user = new ApplicationUser { UserName = request.Username, SageCustomerCode = "CUST-001" };

        userManager.Setup(m => m.FindByNameAsync(request.Username)).ReturnsAsync(user);
        signInManager.Setup(s => s.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Success);
        adapter.Setup(a => a.LoginAsync(request, user.SageCustomerCode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger.Object);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");

        userManager.Setup(m => m.FindByNameAsync(request.Username)).ReturnsAsync((ApplicationUser?)null);

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        adapter.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new LoginRequest("user", "password");
        var user = new ApplicationUser { UserName = request.Username, SageCustomerCode = "CUST-001" };

        userManager.Setup(m => m.FindByNameAsync(request.Username)).ReturnsAsync(user);
        signInManager.Setup(s => s.CheckPasswordSignInAsync(user, request.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        var act = async () => await service.LoginAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        adapter.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_AssignsCustomerCode()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new RegisterRequest("user", "password", "CUST-001");
        ApplicationUser? createdUser = null;

        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        await service.RegisterAsync(request, CancellationToken.None);

        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be(request.Username);
        createdUser.SageCustomerCode.Should().Be(request.SageCustomerCode);
    }

    [Fact]
    public async Task RegisterAsync_WhenCreateFails_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new RegisterRequest("user", "password", "CUST-001");

        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "failed" }));

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        var act = async () => await service.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenCustomerCodeMissing_ThrowsDomainException()
    {
        var adapter = new Mock<IAuthAdapter>();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var logger = Mock.Of<ILogger<AuthService>>();
        var request = new RegisterRequest("user", "password", string.Empty);

        var service = new AuthService(adapter.Object, userManager.Object, signInManager.Object, logger);

        var act = async () => await service.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());
        var logger = Mock.Of<ILogger<SignInManager<ApplicationUser>>>();
        var schemes = Mock.Of<IAuthenticationSchemeProvider>();
        var confirmation = Mock.Of<IUserConfirmation<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(userManager, contextAccessor, claimsFactory, options.Object, logger, schemes, confirmation);
    }
}
