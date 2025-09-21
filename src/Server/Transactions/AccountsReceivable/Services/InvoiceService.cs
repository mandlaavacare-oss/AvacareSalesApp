using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;

namespace Server.Transactions.AccountsReceivable.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceAdapter _adapter;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(IInvoiceAdapter adapter, ILogger<InvoiceService> logger)
    {
        _adapter = adapter;
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = new Invoice(Guid.NewGuid().ToString(), request.CustomerId, request.Amount, request.IssuedOn, request.Status);

        try
        {
            return await _adapter.CreateInvoiceAsync(invoice, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice for customer {CustomerId}", request.CustomerId);
            throw new DomainException("Unable to create invoice in Sage.", ex);
        }
    }
}
