using System.Text;
using Server.Common.Exceptions;
using Server.Infrastructure.Authentication.Models;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.Inventory.Models;
using Server.Transactions.OrderEntry.Models;

namespace Server.Sage;

/// <summary>
/// Provides a deterministic, in-memory representation of the Sage SDK so the
/// application can be exercised without the real integration during tests or demos.
/// </summary>
public static class SageSdkStub
{
    private static readonly object SyncRoot = new();

    private static readonly Dictionary<string, (string Password, string TokenSeed)> UserSeed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["demo"] = ("P@ssw0rd!", "demo-token"),
        ["sales"] = ("Sales123!", "sales-token")
    };

    private static readonly Dictionary<string, Customer> CustomerSeed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["100"] = new Customer("100", "Acme Corporation", "accounts@acme.test", 15_000m),
        ["200"] = new Customer("200", "Globex Corporation", "billing@globex.test", 25_000m)
    };

    private static readonly Dictionary<string, Product> ProductSeed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PROD-001"] = new Product("PROD-001", "Widget", "Standard widget", 49.99m, 50),
        ["PROD-002"] = new Product("PROD-002", "Gadget", "Advanced gadget", 99.50m, 25),
        ["PROD-003"] = new Product("PROD-003", "Doohickey", "Multi-purpose tool", 149.00m, 10)
    };

    private static readonly Dictionary<string, (string Password, string TokenSeed)> Users = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Customer> Customers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Product> Products = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Invoice> Invoices = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Payment> Payments = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SalesOrder> Orders = new(StringComparer.OrdinalIgnoreCase);

    static SageSdkStub()
    {
        Reset();
    }

    public static void Reset()
    {
        lock (SyncRoot)
        {
            Users.Clear();
            foreach (var (username, entry) in UserSeed)
            {
                Users[username] = entry;
            }

            Customers.Clear();
            foreach (var (id, customer) in CustomerSeed)
            {
                Customers[id] = customer;
            }

            Products.Clear();
            foreach (var (id, product) in ProductSeed)
            {
                Products[id] = product;
            }

            Invoices.Clear();
            Payments.Clear();
            Orders.Clear();
        }
    }

    public static bool TryLogin(string username, string password, out LoginResult result)
    {
        lock (SyncRoot)
        {
            if (Users.TryGetValue(username, out var credentials) && credentials.Password == password)
            {
                var tokenPayload = Encoding.UTF8.GetBytes($"{username}:{credentials.TokenSeed}");
                result = new LoginResult(username, Convert.ToBase64String(tokenPayload));
                return true;
            }

            result = default!;
            return false;
        }
    }

    public static Customer? FindCustomer(string customerId)
    {
        lock (SyncRoot)
        {
            return Customers.TryGetValue(customerId, out var customer) ? customer : null;
        }
    }

    public static Invoice SaveInvoice(Invoice invoice)
    {
        lock (SyncRoot)
        {
            if (!Customers.ContainsKey(invoice.CustomerId))
            {
                throw new DomainException($"Customer {invoice.CustomerId} does not exist in Sage.");
            }

            var posted = invoice with { Status = "Posted" };
            Invoices[posted.Id] = posted;
            return posted;
        }
    }

    public static Payment SavePayment(Payment payment)
    {
        lock (SyncRoot)
        {
            if (!Invoices.ContainsKey(payment.InvoiceId))
            {
                throw new DomainException($"Invoice {payment.InvoiceId} was not found in Sage.");
            }

            Payments[payment.Id] = payment;
            return payment;
        }
    }

    public static IReadOnlyCollection<Product> GetProducts()
    {
        lock (SyncRoot)
        {
            return Products.Values.Select(product => product with { }).ToArray();
        }
    }

    public static SalesOrder SaveOrder(SalesOrder order)
    {
        lock (SyncRoot)
        {
            if (!Customers.ContainsKey(order.CustomerId))
            {
                throw new DomainException($"Customer {order.CustomerId} does not exist in Sage.");
            }

            foreach (var line in order.Lines)
            {
                if (!Products.TryGetValue(line.ProductId, out var product))
                {
                    throw new DomainException($"Product {line.ProductId} does not exist in Sage.");
                }

                if (line.Quantity <= 0)
                {
                    throw new DomainException($"Quantity for {line.ProductId} must be greater than zero.");
                }

                var quantity = (int)line.Quantity;
                if (line.Quantity != quantity)
                {
                    throw new DomainException($"Fractional quantities are not supported for {line.ProductId} in the stub.");
                }

                if (quantity > product.QuantityOnHand)
                {
                    throw new DomainException($"Insufficient stock for {line.ProductId}.");
                }

                Products[line.ProductId] = product with { QuantityOnHand = product.QuantityOnHand - quantity };
            }

            Orders[order.Id] = order;
            return order;
        }
    }
}
