using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Server.Infrastructure.Authentication;
using Server.Infrastructure.Authentication.Adapters;
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Avacare Sales API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("Identity"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentitySeedOptions>(builder.Configuration.GetSection("IdentitySeed"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    if (string.IsNullOrWhiteSpace(jwtOptions.Key))
    {
        throw new InvalidOperationException("JWT configuration is missing an encryption key.");
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.RequireAdmin, policy => policy.RequireRole(RoleNames.Admin));
    options.AddPolicy(PolicyNames.RequireCustomer, policy => policy.RequireRole(RoleNames.Customer));
});

builder.Services.AddScoped<IDatabaseContext, DatabaseContext>();

builder.Services.AddScoped<IAuthAdapter, IdentityAuthAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IIdentitySeeder, IdentitySeeder>();

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

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

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

public partial class Program;
