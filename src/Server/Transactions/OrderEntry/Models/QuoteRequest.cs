using System.Linq;
using System.Collections.ObjectModel;

namespace AvacareSalesApp.Transactions.OrderEntry.Models;

public sealed class QuoteRequest
{
    public QuoteRequest(string customerCode, IEnumerable<QuoteLineItem> lineItems)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerCode);
        ArgumentNullException.ThrowIfNull(lineItems);

        CustomerCode = customerCode;
        LineItems = new ReadOnlyCollection<QuoteLineItem>(lineItems.ToList());
    }

    public string CustomerCode { get; }

    public IReadOnlyList<QuoteLineItem> LineItems { get; }
}
