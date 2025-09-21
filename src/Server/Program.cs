using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("IdentityDb"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

if (jwtSettings is null || string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
{
    throw new InvalidOperationException("JWT settings are not configured correctly.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(RoleNames.Admin));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole(RoleNames.Customer));
    options.AddPolicy("RequireAdminOrCustomer", policy => policy.RequireRole(RoleNames.Admin, RoleNames.Customer));
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Avacare Sales API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter 'Bearer {token}' to authenticate. Tokens include role claims for Admin and Customer access."
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

builder.Services.AddScoped<IDatabaseContext, DatabaseContext>();

builder.Services.AddScoped<IAuthAdapter, SageAuthAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IdentityDataSeeder>();

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
    var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync();
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

await app.RunAsync();
