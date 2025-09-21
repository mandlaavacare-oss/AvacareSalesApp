using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;
using Server.Infrastructure.Database;

namespace Server.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDatabaseContext _databaseContext;
    private readonly IUserOnboardingService _userOnboardingService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IUserOnboardingService userOnboardingService, IDatabaseContext databaseContext, ILogger<AuthController> logger)
    {
        _authService = authService;
        _userOnboardingService = userOnboardingService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(result);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Failed login attempt for {Username}", request.Username);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error during login for {Username}", request.Username);
            return Problem("An unexpected error occurred while logging in.");
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var result = await _userOnboardingService.RegisterAsync(request, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(result);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Failed registration attempt for {Username}", request.Username);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error during registration for {Username}", request.Username);
            return Problem("An unexpected error occurred while registering.");
        }
    }
}
