using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Transactions.Inventory.Adapters;
using Server.Transactions.Inventory.Models;

namespace Server.Transactions.Inventory.Services;

public interface IProductService
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken);
}

public class ProductService : IProductService
{
    private readonly IProductAdapter _adapter;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductAdapter adapter, ILogger<ProductService> logger)
    {
        _adapter = adapter;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _adapter.GetProductsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load products from Sage");
            throw new DomainException("Unable to retrieve products from Sage.", ex);
        }
    }
}
