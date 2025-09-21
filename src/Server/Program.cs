using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Infrastructure.Authentication;
using Server.Infrastructure.Authentication.Adapters;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=avacare.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IDatabaseContext, DatabaseContext>();

builder.Services.AddScoped<IAuthAdapter, SageAuthAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserOnboardingService, UserOnboardingService>();

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

app.MapControllers();

app.Run();
