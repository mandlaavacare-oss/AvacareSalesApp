using System.Linq;
using AvacareSalesApp.Transactions.OrderEntry.Controllers.Requests;
using AvacareSalesApp.Transactions.OrderEntry.Models;
using AvacareSalesApp.Transactions.OrderEntry.Services;
using Microsoft.AspNetCore.Mvc;

namespace AvacareSalesApp.Transactions.OrderEntry.Controllers;

[ApiController]
[Route("transactions/order-entry/quotes")]
public sealed class QuotePricingController : ControllerBase
{
    private readonly QuotePricingService quotePricingService;

    public QuotePricingController(QuotePricingService quotePricingService)
    {
        this.quotePricingService = quotePricingService;
    }

    [HttpPost("price")]
    [ProducesResponseType(typeof(QuotePricingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CalculateQuote([FromBody] QuotePricingRequestDto request)
    {
        if (request is null)
        {
            return BadRequest("A quote request body must be supplied.");
        }

        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            return BadRequest("At least one line item must be provided.");
        }

        try
        {
            var domainRequest = new QuoteRequest(
                request.CustomerCode,
                request.LineItems.Select(line => new QuoteLineItem(line.Sku, line.Quantity)));

            var result = quotePricingService.CalculateTotals(domainRequest);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
