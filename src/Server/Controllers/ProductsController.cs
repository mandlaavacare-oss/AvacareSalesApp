using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Common.Exceptions;
using Server.Infrastructure.Database;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Services;

namespace Server.Controllers;

[ApiController]
[Route("products")]
[Authorize(Policy = "RequireAdminOrCustomer")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, IDatabaseContext databaseContext, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _databaseContext = databaseContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Product>>> GetProducts(CancellationToken cancellationToken)
    {
        _databaseContext.BeginTran();

        try
        {
            var products = await _productService.GetProductsAsync(cancellationToken);
            _databaseContext.CommitTran();
            return Ok(products);
        }
        catch (DomainException ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Domain error when retrieving products");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _databaseContext.RollbackTran();
            _logger.LogError(ex, "Unhandled error when retrieving products");
            return Problem("An unexpected error occurred while retrieving products.");
        }
    }
}
