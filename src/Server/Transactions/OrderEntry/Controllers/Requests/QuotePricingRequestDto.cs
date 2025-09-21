namespace AvacareSalesApp.Transactions.OrderEntry.Controllers.Requests;

public sealed class QuotePricingRequestDto
{
    public string CustomerCode { get; set; } = string.Empty;

    public List<QuoteLineItemDto> LineItems { get; set; } = new();
}

public sealed class QuoteLineItemDto
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
