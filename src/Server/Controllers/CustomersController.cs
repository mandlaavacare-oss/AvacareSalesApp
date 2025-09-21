using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;
using Server.Infrastructure.Authentication;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, IDatabaseContext databaseContext, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Customer)]
    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(string id, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Customer))
        {
            var customerId = User.FindFirstValue(IdentityClaimTypes.CustomerId);
            if (!string.Equals(customerId, id, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        _databaseContext.BeginTran();

        try
        {
            var customer = await _customerService.GetCustomerAsync(id, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(customer);
        }
        catch (NotFoundException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Customer {CustomerId} was not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Domain error when retrieving customer {CustomerId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error when retrieving customer {CustomerId}", id);
            return Problem("An unexpected error occurred while retrieving the customer.");
        }
    }
}
