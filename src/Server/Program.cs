using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Authentication.Adapters;
using Server.Infrastructure.Authentication.Database;
using Server.Infrastructure.Authentication.Models;
using Server.Infrastructure.Authentication.Services;
using Server.Infrastructure.Database;
using Server.Transactions.AccountsReceivable.Adapters;
using Server.Transactions.AccountsReceivable.Services;
using Server.Transactions.Inventory.Adapters;
using Server.Transactions.Inventory.Services;
using Server.Transactions.OrderEntry.Adapters;
using Server.Transactions.OrderEntry.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<IDatabaseContext, DatabaseContext>();

builder.Services.AddScoped<IAuthAdapter, SageAuthAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICustomerAdapter, SageCustomerAdapter>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IInvoiceAdapter, SageInvoiceAdapter>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

builder.Services.AddScoped<IPaymentAdapter, SagePaymentAdapter>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IProductAdapter, SageProductAdapter>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IOrderEntryAdapter, SageOrderEntryAdapter>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
