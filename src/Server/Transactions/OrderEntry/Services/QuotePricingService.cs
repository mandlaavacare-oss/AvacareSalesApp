using AvacareSalesApp.Transactions.OrderEntry.Adapters;
using AvacareSalesApp.Transactions.OrderEntry.Models;

namespace AvacareSalesApp.Transactions.OrderEntry.Services;

public sealed class QuotePricingService
{
    private readonly IProductCatalogAdapter productCatalogAdapter;
    private const decimal TaxRate = 0.15m; // 15% VAT

    public QuotePricingService(IProductCatalogAdapter productCatalogAdapter)
    {
        this.productCatalogAdapter = productCatalogAdapter;
    }

    public QuotePricingResult CalculateTotals(QuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.LineItems.Count == 0)
        {
            throw new ArgumentException("At least one line item is required to price a quote.", nameof(request));
        }

        var lines = new List<QuoteLineItemTotal>(request.LineItems.Count);
        decimal subtotal = 0m;

        foreach (var lineItem in request.LineItems)
        {
            if (lineItem.Quantity <= 0)
            {
                throw new ArgumentException("Line item quantities must be greater than zero.", nameof(request));
            }

            var unitPrice = productCatalogAdapter.GetUnitPrice(lineItem.Sku);
            var lineTotal = Math.Round(unitPrice * lineItem.Quantity, 2, MidpointRounding.AwayFromZero);
            subtotal += lineTotal;

            lines.Add(new QuoteLineItemTotal(lineItem.Sku, lineItem.Quantity, unitPrice, lineTotal));
        }

        subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        var tax = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;

        return new QuotePricingResult(request.CustomerCode, subtotal, tax, total, lines);
    }
}
