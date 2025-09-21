using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Infrastructure.Authentication.Models;
using Server.Transactions.AccountsReceivable.Models;
using Server.Transactions.AccountsReceivable.Services;
using Server.Transactions.OrderEntry.Models;
using Server.Transactions.OrderEntry.Services;

namespace Server.Tests.Integration.Authorization;

public class AuthorizationTests : IClassFixture<AuthorizationTests.CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminToken_AllowsOrderCreation()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "admin@avacare.local", "Admin123$");

        var request = new CreateOrderRequest("CUST-0001", DateTime.UtcNow, new[] { new SalesOrderLine("SKU-1", 1, 10m) });
        var httpRequest = JsonContent.Create(request);
        var httpMessage = new HttpRequestMessage(HttpMethod.Post, "/orders") { Content = httpRequest };
        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(httpMessage);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CustomerToken_BlockedFromOrderCreation()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "customer@avacare.local", "Customer123$");

        var request = new CreateOrderRequest("CUST-0001", DateTime.UtcNow, new[] { new SalesOrderLine("SKU-1", 1, 10m) });
        var httpMessage = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(request)
        };
        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(httpMessage);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustomerToken_AllowsAccessToOwnRecord()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "customer@avacare.local", "Customer123$");

        var request = new HttpRequestMessage(HttpMethod.Get, "/customers/CUST-0001");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CustomerToken_BlockedFromOtherCustomer()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client, "customer@avacare.local", "Customer123$");

        var request = new HttpRequestMessage(HttpMethod.Get, "/customers/CUST-9999");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var loginResult = await JsonSerializer.DeserializeAsync<LoginResponse>(contentStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return loginResult?.Token ?? throw new InvalidOperationException("Token was not returned by login endpoint.");
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IOrderService));
                services.AddSingleton<IOrderService>(new FakeOrderService());

                services.RemoveAll(typeof(ICustomerService));
                services.AddSingleton<ICustomerService>(new FakeCustomerService());
            });
        }
    }

    private class FakeOrderService : IOrderService
    {
        public Task<SalesOrder> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            var order = new SalesOrder("ORDER-1", request.CustomerId, request.OrderDate, request.Lines);
            return Task.FromResult(order);
        }
    }

    private class FakeCustomerService : ICustomerService
    {
        public Task<Customer> GetCustomerAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Customer(customerId, "Acme Corp", "customer@avacare.local", 1000m));
        }
    }

    private record LoginResponse(string Username, string Token);
}
