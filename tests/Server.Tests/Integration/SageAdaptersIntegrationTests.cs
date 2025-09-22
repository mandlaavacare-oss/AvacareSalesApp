using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Controllers;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;
using Server.Infrastructure.Database;
using Server.Sage;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;
using Server.Transactions.Inventory.Adapters;
using Server.Transactions.Inventory.Models;
using Server.Transactions.Inventory.Services;
using Server.Transactions.OrderEntry.Adapters;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Services;

namespace Server.Tests.Integration;

public class SageAdaptersIntegrationTests
{
    private static readonly DateTime SampleDate = new(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AuthController_WithValidCredentials_ReturnsToken()
    {
        SageSdkStub.Reset();

        var controller = BuildAuthController(out var databaseContext);
        var result = await controller.Login(new LoginRequest("demo", "P@ssw0rd!"), CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var login = okResult.Value.Should().BeOfType<LoginResult>().Subject;
        login.Username.Should().Be("demo");
        login.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CustomersController_WithExistingCustomer_ReturnsCustomer()
    {
        SageSdkStub.Reset();

        var controller = BuildCustomersController(out var databaseContext);
        var response = await controller.GetCustomer("100", CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new Customer("100", "Acme Corporation", "accounts@acme.test", 15_000m));
    }

    [Fact]
    public async Task InvoicesController_WhenPostingInvoice_ReturnsPostedInvoice()
    {
        SageSdkStub.Reset();

        var controller = BuildInvoicesController(out var databaseContext);
        var request = new CreateInvoiceRequest("100", 1250m, SampleDate, "Draft");

        var response = await controller.CreateInvoice(request, CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var invoice = okResult.Value.Should().BeOfType<Invoice>().Subject;
        invoice.CustomerId.Should().Be("100");
        invoice.Amount.Should().Be(1250m);
        invoice.Status.Should().Be("Posted");
    }

    [Fact]
    public async Task PaymentsController_WhenApplyingPayment_ReturnsPayment()
    {
        SageSdkStub.Reset();

        var invoicesController = BuildInvoicesController(out _);
        var invoiceResponse = await invoicesController.CreateInvoice(
            new CreateInvoiceRequest("100", 500m, SampleDate, "Draft"),
            CancellationToken.None);
        var invoiceResult = invoiceResponse.Result.Should().BeOfType<OkObjectResult>().Subject;
        var invoice = invoiceResult.Value.Should().BeOfType<Invoice>().Subject;

        var paymentsController = BuildPaymentsController(out var databaseContext);
        var paymentRequest = new ApplyPaymentRequest(invoice.Id, 200m, SampleDate.AddDays(1));

        var paymentResponse = await paymentsController.ApplyPayment(paymentRequest, CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = paymentResponse.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payment = okResult.Value.Should().BeOfType<Payment>().Subject;
        payment.InvoiceId.Should().Be(invoice.Id);
        payment.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task ProductsController_ReturnsInventorySnapshot()
    {
        SageSdkStub.Reset();

        var controller = BuildProductsController(out var databaseContext);
        var response = await controller.GetProducts(CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var products = okResult.Value.Should().BeAssignableTo<IReadOnlyCollection<Product>>().Subject;
        products.Should().Contain(p => p.Id == "PROD-001" && p.QuantityOnHand == 50);
    }

    [Fact]
    public async Task OrdersController_WhenCreatingOrder_AdjustsInventory()
    {
        SageSdkStub.Reset();

        var controller = BuildOrdersController(out var databaseContext);
        var orderRequest = new CreateOrderRequest(
            "100",
            SampleDate,
            new[] { new SalesOrderLine("PROD-001", 2m, 49.99m) });

        var response = await controller.CreateOrder(orderRequest, CancellationToken.None);

        databaseContext.BeginCount.Should().Be(1);
        databaseContext.CommitCount.Should().Be(1);
        databaseContext.RollbackCount.Should().Be(0);

        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var order = okResult.Value.Should().BeOfType<SalesOrder>().Subject;
        order.CustomerId.Should().Be("100");
        order.Lines.Should().ContainSingle(line => line.ProductId == "PROD-001" && line.Quantity == 2m);

        var productsController = BuildProductsController(out _);
        var productsResult = await productsController.GetProducts(CancellationToken.None);
        var productsOk = productsResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var products = productsOk.Value.Should().BeAssignableTo<IReadOnlyCollection<Product>>().Subject;
        products.Should().Contain(p => p.Id == "PROD-001" && p.QuantityOnHand == 48);
    }

    private static AuthController BuildAuthController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SageAuthAdapter();
        var service = new AuthService(adapter, NullLogger<AuthService>.Instance);
        return new AuthController(service, databaseContext, NullLogger<AuthController>.Instance);
    }

    private static CustomersController BuildCustomersController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SageCustomerAdapter();
        var service = new CustomerService(adapter, NullLogger<CustomerService>.Instance);
        return new CustomersController(service, databaseContext, NullLogger<CustomersController>.Instance);
    }

    private static InvoicesController BuildInvoicesController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SageInvoiceAdapter();
        var service = new InvoiceService(adapter, NullLogger<InvoiceService>.Instance);
        return new InvoicesController(service, databaseContext, NullLogger<InvoicesController>.Instance);
    }

    private static PaymentsController BuildPaymentsController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SagePaymentAdapter();
        var service = new PaymentService(adapter, NullLogger<PaymentService>.Instance);
        return new PaymentsController(service, databaseContext, NullLogger<PaymentsController>.Instance);
    }

    private static ProductsController BuildProductsController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SageProductAdapter();
        var service = new ProductService(adapter, NullLogger<ProductService>.Instance);
        return new ProductsController(service, databaseContext, NullLogger<ProductsController>.Instance);
    }

    private static OrdersController BuildOrdersController(out FakeDatabaseContext databaseContext)
    {
        databaseContext = new FakeDatabaseContext();
        var adapter = new SageOrderEntryAdapter();
        var service = new OrderService(adapter, NullLogger<OrderService>.Instance);
        return new OrdersController(service, databaseContext, NullLogger<OrdersController>.Instance);
    }

    private sealed class FakeDatabaseContext : IDatabaseContext
    {
        public int BeginCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public void BeginTran() => BeginCount++;

        public void CommitTran() => CommitCount++;

        public void RollbackTran() => RollbackCount++;
    }
}
