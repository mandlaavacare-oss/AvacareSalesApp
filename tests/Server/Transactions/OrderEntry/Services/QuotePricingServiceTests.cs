using System.Collections.Generic;
using AvacareSalesApp.Transactions.OrderEntry.Adapters;
using AvacareSalesApp.Transactions.OrderEntry.Models;
using AvacareSalesApp.Transactions.OrderEntry.Services;

namespace AvacareSalesApp.Tests.Transactions.OrderEntry.Services;

public sealed class QuotePricingServiceTests
{
    [Fact]
    public void CalculateTotals_ReturnsExpectedBreakdown()
    {
        var adapter = new FakeProductCatalogAdapter(new Dictionary<string, decimal>
        {
            ["ITEM-1001"] = 1250.00m,
            ["ITEM-2002"] = 499.99m
        });
        var service = new QuotePricingService(adapter);
        var request = new QuoteRequest("CUST-01", new[]
        {
            new QuoteLineItem("ITEM-1001", 2),
            new QuoteLineItem("ITEM-2002", 1)
        });

        var result = service.CalculateTotals(request);

        Assert.Equal("CUST-01", result.CustomerCode);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(2, result.Lines[0].Quantity);
        Assert.Equal(1250.00m, result.Lines[0].UnitPrice);
        Assert.Equal(2500.00m, result.Lines[0].LineTotal);
        Assert.Equal(499.99m, result.Lines[1].LineTotal);

        var expectedSubtotal = 2999.99m;
        Assert.Equal(expectedSubtotal, result.Subtotal);

        var expectedTax = Math.Round(expectedSubtotal * 0.15m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedTax, result.Tax);
        Assert.Equal(expectedSubtotal + expectedTax, result.Total);
    }

    [Fact]
    public void CalculateTotals_ThrowsWhenLineQuantityIsZero()
    {
        var adapter = new FakeProductCatalogAdapter(new Dictionary<string, decimal>
        {
            ["ITEM-1001"] = 100m
        });
        var service = new QuotePricingService(adapter);
        var request = new QuoteRequest("CUST-02", new[] { new QuoteLineItem("ITEM-1001", 0) });

        var ex = Assert.Throws<ArgumentException>(() => service.CalculateTotals(request));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void CalculateTotals_ThrowsWhenSkuMissing()
    {
        var adapter = new FakeProductCatalogAdapter(new Dictionary<string, decimal>());
        var service = new QuotePricingService(adapter);
        var request = new QuoteRequest("CUST-03", new[] { new QuoteLineItem("ITEM-999", 1) });

        Assert.Throws<KeyNotFoundException>(() => service.CalculateTotals(request));
    }

    private sealed class FakeProductCatalogAdapter : IProductCatalogAdapter
    {
        private readonly IReadOnlyDictionary<string, decimal> prices;

        public FakeProductCatalogAdapter(IReadOnlyDictionary<string, decimal> prices)
        {
            this.prices = prices;
        }

        public decimal GetUnitPrice(string sku)
        {
            if (!prices.TryGetValue(sku, out var price))
            {
                throw new KeyNotFoundException($"SKU '{sku}' not found in fake catalog.");
            }

            return price;
        }
    }
}
