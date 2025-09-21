namespace AvacareSalesApp.Transactions.OrderEntry.Adapters;

public interface IProductCatalogAdapter
{
    decimal GetUnitPrice(string sku);
}
