using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Controllers;

[ApiController]
[Route("invoices")]
[Authorize(Policy = "RequireAdmin")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, IDatabaseContext databaseContext, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Invoice>> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(invoice);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Domain error when creating invoice for customer {CustomerId}", request.CustomerId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error when creating invoice for customer {CustomerId}", request.CustomerId);
            return Problem("An unexpected error occurred while creating the invoice.");
        }
    }
}
