namespace AvacareSalesApp.Transactions.OrderEntry.Models;

public sealed class QuotePricingResult
{
    public QuotePricingResult(string customerCode, decimal subtotal, decimal tax, decimal total, IReadOnlyList<QuoteLineItemTotal> lines)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerCode);
        ArgumentNullException.ThrowIfNull(lines);

        CustomerCode = customerCode;
        Subtotal = subtotal;
        Tax = tax;
        Total = total;
        Lines = lines;
    }

    public string CustomerCode { get; }

    public decimal Subtotal { get; }

    public decimal Tax { get; }

    public decimal Total { get; }

    public IReadOnlyList<QuoteLineItemTotal> Lines { get; }
}
