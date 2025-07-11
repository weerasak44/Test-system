using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Using singleton in-memory repositories for demo purposes. Replace with EF Core in production.
builder.Services.AddSingleton<Repositories>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/auth/login", (LoginRequest req, Repositories repo) =>
{
    var user = repo.Users.FirstOrDefault(u => u.Username == req.Username && u.Password == req.Password);
    return user is null ? Results.Unauthorized() : Results.Ok(user);
});

// Product endpoints
app.MapGet("/products", (Repositories repo) => repo.Products);
app.MapGet("/products/{id}", (int id, Repositories repo) =>
    repo.Products.FirstOrDefault(p => p.Id == id) is Product p ? Results.Ok(p) : Results.NotFound());
app.MapPost("/products", (ProductCreateRequest req, Repositories repo) =>
{
    var product = new Product
    {
        Id = repo.NextProductId++,
        Code = req.Code,
        Name = req.Name,
        CostPrice = req.CostPrice,
        Unit = req.Unit,
        NormalPrice = req.NormalPrice,
        StaffPrice = req.StaffPrice,
        WholesalePrice = req.WholesalePrice,
        Stock = req.Stock
    };
    repo.Products.Add(product);
    return Results.Created($"/products/{product.Id}", product);
});
app.MapPut("/products/{id}", (int id, ProductUpdateRequest req, Repositories repo) =>
{
    var product = repo.Products.FirstOrDefault(p => p.Id == id);
    if (product is null) return Results.NotFound();

    product.Code = req.Code ?? product.Code;
    product.Name = req.Name ?? product.Name;
    product.CostPrice = req.CostPrice ?? product.CostPrice;
    product.Unit = req.Unit ?? product.Unit;
    product.NormalPrice = req.NormalPrice ?? product.NormalPrice;
    product.StaffPrice = req.StaffPrice ?? product.StaffPrice;
    product.WholesalePrice = req.WholesalePrice ?? product.WholesalePrice;
    product.Stock = req.Stock ?? product.Stock;

    return Results.NoContent();
});
app.MapDelete("/products/{id}", (int id, Repositories repo) =>
{
    var product = repo.Products.FirstOrDefault(p => p.Id == id);
    if (product is null) return Results.NotFound();
    repo.Products.Remove(product);
    return Results.NoContent();
});

// Member endpoints
app.MapGet("/members", (Repositories repo) => repo.Members);
app.MapPost("/members", (MemberCreateRequest req, Repositories repo) =>
{
    var member = new Member
    {
        Id = repo.NextMemberId++,
        Name = req.Name,
        Phone = req.Phone,
        Credit = 0m
    };
    repo.Members.Add(member);
    return Results.Created($"/members/{member.Id}", member);
});

// Sale endpoints
app.MapPost("/sales", (SaleCreateRequest req, Repositories repo) =>
{
    var sale = new Sale
    {
        Id = repo.NextSaleId++,
        Timestamp = DateTime.UtcNow,
        CashierId = req.CashierId,
        Items = new List<SaleItem>(),
        PaymentType = req.PaymentType,
        PriceLevel = req.PriceLevel
    };

    foreach (var itemReq in req.Items)
    {
        var product = repo.Products.FirstOrDefault(p => p.Id == itemReq.ProductId);
        if (product is null) return Results.BadRequest($"Product {itemReq.ProductId} not found");
        if (product.Stock < itemReq.Quantity) return Results.BadRequest($"Stock insufficient for {product.Name}");

        decimal sellingPrice = sale.PriceLevel switch
        {
            PriceLevel.Normal => product.NormalPrice,
            PriceLevel.Staff => product.StaffPrice,
            PriceLevel.Wholesale => product.WholesalePrice,
            _ => product.NormalPrice
        };

        sale.Items.Add(new SaleItem
        {
            ProductId = product.Id,
            Quantity = itemReq.Quantity,
            UnitPrice = sellingPrice
        });

        product.Stock -= itemReq.Quantity;
    }

    sale.Total = sale.Items.Sum(i => i.Quantity * i.UnitPrice);

    repo.Sales.Add(sale);
    return Results.Created($"/sales/{sale.Id}", sale);
});

// Simple sales report
app.MapGet("/reports/sales", (DateTime from, DateTime to, Repositories repo) =>
{
    var sales = repo.Sales.Where(s => s.Timestamp >= from && s.Timestamp <= to).ToList();
    var total = sales.Sum(s => s.Total);
    var profit = sales.Sum(s => s.Items.Sum(i =>
    {
        var product = repo.Products.FirstOrDefault(p => p.Id == i.ProductId);
        return (i.UnitPrice - (product?.CostPrice ?? 0)) * i.Quantity;
    }));
    return Results.Ok(new { from, to, total, profit, count = sales.Count });
});

app.Run();

// --------------------- Models & DTOs ---------------------
record LoginRequest(string Username, string Password);

record Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal NormalPrice { get; set; }
    public decimal StaffPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int Stock { get; set; }
}

record ProductCreateRequest(string Code, string Name, decimal CostPrice, string Unit, decimal NormalPrice, decimal StaffPrice, decimal WholesalePrice, int Stock);

record ProductUpdateRequest(string? Code, string? Name, decimal? CostPrice, string? Unit, decimal? NormalPrice, decimal? StaffPrice, decimal? WholesalePrice, int? Stock);

record Member
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Credit { get; set; }
}

record MemberCreateRequest(string Name, string? Phone);

enum PaymentType { Cash, Transfer, Credit }

enum PriceLevel { Normal, Staff, Wholesale }

record SaleItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

record Sale
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int CashierId { get; set; }
    public List<SaleItem> Items { get; set; } = new();
    public PaymentType PaymentType { get; set; }
    public PriceLevel PriceLevel { get; set; }
    public decimal Total { get; set; }
}

record SaleItemRequest(int ProductId, int Quantity);

record SaleCreateRequest(int CashierId, List<SaleItemRequest> Items, PaymentType PaymentType, PriceLevel PriceLevel);

record User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // For demo only. Hash in production
    public Role Role { get; set; }
}

enum Role { Admin, Cashier }

// --------------------- Repositories ---------------------
class Repositories
{
    public List<User> Users { get; } = new();
    public List<Product> Products { get; } = new();
    public List<Member> Members { get; } = new();
    public List<Sale> Sales { get; } = new();

    public int NextProductId { get; set; } = 1;
    public int NextMemberId { get; set; } = 1;
    public int NextSaleId { get; set; } = 1;

    public Repositories()
    {
        // Seed an admin user
        Users.Add(new User { Id = 1, Username = "admin", Password = "admin", Role = Role.Admin });
    }
}