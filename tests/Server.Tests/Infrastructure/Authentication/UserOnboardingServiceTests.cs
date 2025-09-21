using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;

namespace Server.Tests.Infrastructure.Authentication;

public class UserOnboardingServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenSuccessful_ReturnsRegistrationResult()
    {
        var userManager = CreateUserManagerMock();
        var logger = Mock.Of<ILogger<UserOnboardingService>>();
        var request = new RegisterRequest("user", "Password123!", "CUST001");
        ApplicationUser? createdUser = null;

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        var service = new UserOnboardingService(userManager.Object, logger);

        var result = await service.RegisterAsync(request, CancellationToken.None);

        result.Username.Should().Be(request.Username);
        result.SageCustomerCode.Should().Be(request.SageCustomerCode);
        createdUser.Should().NotBeNull();
        createdUser!.SageCustomerCode.Should().Be(request.SageCustomerCode);
    }

    [Fact]
    public async Task RegisterAsync_WhenCustomerCodeMissing_ThrowsDomainException()
    {
        var userManager = CreateUserManagerMock();
        var logger = Mock.Of<ILogger<UserOnboardingService>>();
        var request = new RegisterRequest("user", "Password123!", " ");

        var service = new UserOnboardingService(userManager.Object, logger);

        var act = async () => await service.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenUserExists_ThrowsDomainException()
    {
        var userManager = CreateUserManagerMock();
        var logger = Mock.Of<ILogger<UserOnboardingService>>();
        var request = new RegisterRequest("user", "Password123!", "CUST001");

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync(new ApplicationUser());

        var service = new UserOnboardingService(userManager.Object, logger);

        var act = async () => await service.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task RegisterAsync_WhenIdentityFails_ThrowsDomainException()
    {
        var userManager = CreateUserManagerMock();
        var logger = Mock.Of<ILogger<UserOnboardingService>>();
        var request = new RegisterRequest("user", "Password123!", "CUST001");

        userManager.Setup(m => m.FindByNameAsync(request.Username))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var service = new UserOnboardingService(userManager.Object, logger);

        var act = async () => await service.RegisterAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
