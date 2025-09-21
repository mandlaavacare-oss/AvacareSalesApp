using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Services;

public interface IPaymentService
{
    Task<Payment> ApplyPaymentAsync(ApplyPaymentRequest request, CancellationToken cancellationToken);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentAdapter _adapter;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentAdapter adapter, ILogger<PaymentService> logger)
    {
        _adapter = adapter;
        _logger = logger;
    }

    public async Task<Payment> ApplyPaymentAsync(ApplyPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = new Payment(Guid.NewGuid().ToString(), request.InvoiceId, request.Amount, request.PaidOn);

        try
        {
            return await _adapter.ApplyPaymentAsync(payment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply payment for invoice {InvoiceId}", request.InvoiceId);
            throw new DomainException("Unable to post payment in Sage.", ex);
        }
    }
}
