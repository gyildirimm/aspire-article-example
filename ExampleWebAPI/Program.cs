using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure PostgreSQL database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Product API",
        Description = "A simple API to manage products",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    dbContext.Database.EnsureCreated();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);
    var retries = 0;
    var databaseReady = false;

    while (retries < maxRetries && !databaseReady)
    {
        try
        {
            logger.LogInformation("Checking database health...");
            dbContext.Database.OpenConnection();
            dbContext.Database.CloseConnection();
            databaseReady = true;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning($"Database not ready. Retrying {retries}/{maxRetries}...");
            if (retries == maxRetries)
            {
                throw new Exception("Database not ready after maximum retries.", ex);
            }
            await Task.Delay(delay);
        }
    }

    //if (databaseReady)
    //{
    //    dbContext.Database.Migrate();
    //}
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1"));
}

app.MapPost("/products", async (ProductDbContext context, Product product) =>
{
    if (string.IsNullOrEmpty(product.Name) || product.Price <= 0)
    {
        return Results.BadRequest("Invalid product data.");
    }

    context.Products.Add(product);
    await context.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", product);
})
.WithName("AddProduct")
.WithTags("Products")
.WithOpenApi();

app.MapGet("/products/{id}", async (ProductDbContext context, int id) =>
{
    var product = await context.Products.FindAsync(id);
    return product == null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProductById")
.WithTags("Products")
.WithOpenApi();

app.Run();
