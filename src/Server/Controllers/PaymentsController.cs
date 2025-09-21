using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;

namespace Server.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, IDatabaseContext databaseContext, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> ApplyPayment([FromBody] ApplyPaymentRequest request, CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var payment = await _paymentService.ApplyPaymentAsync(request, cancellationToken);
            _databaseContext.CommitTran();
            return Ok(payment);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogWarning(ex, "Domain error when applying payment for invoice {InvoiceId}", request.InvoiceId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error when applying payment for invoice {InvoiceId}", request.InvoiceId);
            return Problem("An unexpected error occurred while applying the payment.");
        }
    }
}
