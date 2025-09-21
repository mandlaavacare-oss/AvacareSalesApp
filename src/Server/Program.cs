using AvacareSalesApp.Transactions.OrderEntry.Adapters;
using AvacareSalesApp.Transactions.OrderEntry.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IProductCatalogAdapter, InMemoryProductCatalogAdapter>();
builder.Services.AddScoped<QuotePricingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;
