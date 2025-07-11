using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});
// Register application services
builder.Services.AddSingleton<AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

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
app.MapPut("/members/{id}", (int id, MemberUpdateRequest req, Repositories repo) =>
{
    var member = repo.Members.FirstOrDefault(m => m.Id == id);
    if (member is null) return Results.NotFound();
    member.Name = req.Name ?? member.Name;
    member.Phone = req.Phone ?? member.Phone;
    return Results.NoContent();
});
app.MapDelete("/members/{id}", (int id, Repositories repo) =>
{
    var member = repo.Members.FirstOrDefault(m => m.Id == id);
    if (member is null) return Results.NotFound();
    repo.Members.Remove(member);
    return Results.NoContent();
});

// User endpoints
app.MapGet("/users", (Repositories repo) => repo.Users);
app.MapPost("/users", (UserCreateRequest req, Repositories repo) =>
{
    var user = new User { Id = repo.NextUserId++, Username = req.Username, Password = req.Password, Role = req.Role };
    repo.Users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});
app.MapPut("/users/{id}", (int id, UserUpdateRequest req, Repositories repo) =>
{
    var user = repo.Users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    user.Username = req.Username ?? user.Username;
    if (!string.IsNullOrWhiteSpace(req.Password)) user.Password = req.Password!;
    user.Role = req.Role ?? user.Role;
    return Results.NoContent();
});
app.MapDelete("/users/{id}", (int id, Repositories repo) =>
{
    var user = repo.Users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    repo.Users.Remove(user);
    return Results.NoContent();
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

// CSV export for sales report
app.MapGet("/reports/sales/export", (DateTime from, DateTime to, Repositories repo) =>
{
    var sales = repo.Sales.Where(s => s.Timestamp >= from && s.Timestamp <= to).ToList();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("SaleId,Timestamp,CashierId,Total");
    foreach (var s in sales)
        sb.AppendLine($"{s.Id},{s.Timestamp:o},{s.CashierId},{s.Total}");
    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    string filename = $"sales_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
    return Results.File(bytes, "text/csv", filename);
});

// Stock report
app.MapGet("/reports/stock", (Repositories repo) =>
{
    var totalValue = repo.Products.Sum(p => p.Stock * p.CostPrice);
    return Results.Ok(new { totalValue, products = repo.Products });
});

// CSV export for stock
app.MapGet("/reports/stock/export", (Repositories repo) =>
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("Id,Code,Name,Stock,CostPrice,NormalPrice,StaffPrice,WholesalePrice");
    foreach (var p in repo.Products)
        sb.AppendLine($"{p.Id},{p.Code},{p.Name},{p.Stock},{p.CostPrice},{p.NormalPrice},{p.StaffPrice},{p.WholesalePrice}");
    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    return Results.File(bytes, "text/csv", "stock_report.csv");
});

// Get sale by id
app.MapGet("/sales/{id}", (int id, Repositories repo) =>
    repo.Sales.FirstOrDefault(s => s.Id == id) is Sale sale ? Results.Ok(sale) : Results.NotFound());

// Receipt generation (plain text)
app.MapGet("/sales/{id}/receipt", (int id, Repositories repo) =>
{
    var sale = repo.Sales.FirstOrDefault(s => s.Id == id);
    if (sale is null) return Results.NotFound();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("*** Receipt ***");
    sb.AppendLine($"Sale #: {sale.Id}");
    sb.AppendLine($"Date : {sale.Timestamp:yyyy-MM-dd HH:mm}");
    sb.AppendLine($"Cashier: {sale.CashierId}");
    sb.AppendLine("------------------------");
    var repoObj = repo;
    foreach (var item in sale.Items)
    {
        var product = repoObj.Products.FirstOrDefault(p => p.Id == item.ProductId);
        string name = product?.Name ?? "#";
        sb.AppendLine($"{name} x{item.Quantity} @ {item.UnitPrice:C} = {item.Quantity * item.UnitPrice:C}");
    }
    sb.AppendLine("------------------------");
    sb.AppendLine($"TOTAL: {sale.Total:C}");
    sb.AppendLine($"Payment: {sale.PaymentType}");
    sb.AppendLine("Thank you!");
    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    return Results.File(bytes, "text/plain", $"receipt_{sale.Id}.txt");
});

// Receipt generation (PDF)
app.MapGet("/sales/{id}/receipt/pdf", (int id, Repositories repo) =>
{
    var sale = repo.Sales.FirstOrDefault(s => s.Id == id);
    if (sale is null) return Results.NotFound();
    var doc = new ReceiptDocument(sale, repo.Products.ToList());
    var pdfBytes = doc.GeneratePdf();
    return Results.File(pdfBytes, "application/pdf", $"receipt_{sale.Id}.pdf");
});

// Low stock list
app.MapGet("/reports/stock/low", (int threshold, Repositories repo) =>
    repo.Products.Where(p => p.Stock <= threshold).ToList());

// Profit by product report
app.MapGet("/reports/profit/product", (DateTime from, DateTime to, Repositories repo) =>
{
    var querySales = repo.Sales.Where(s => s.Timestamp >= from && s.Timestamp <= to);
    var dict = new Dictionary<int, ProfitEntry>();
    foreach (var sale in querySales)
    {
        foreach (var item in sale.Items)
        {
            var product = repo.Products.FirstOrDefault(p => p.Id == item.ProductId);
            var profit = (item.UnitPrice - (product?.CostPrice ?? 0)) * item.Quantity;
            if (!dict.TryGetValue(item.ProductId, out var entry))
            {
                entry = new ProfitEntry(item.ProductId, product?.Name ?? "#", 0, 0m);
            }
            entry = entry with { Quantity = entry.Quantity + item.Quantity, Profit = entry.Profit + profit };
            dict[item.ProductId] = entry;
        }
    }
    return Results.Ok(dict.Values.OrderByDescending(e => e.Profit));
});

// CSV export profit by product
app.MapGet("/reports/profit/product/export", (DateTime from, DateTime to, Repositories repo) =>
{
    var url = $"/reports/profit/product?from={from:O}&to={to:O}"; // reuse logic
    var querySales = repo.Sales.Where(s => s.Timestamp >= from && s.Timestamp <= to);
    var dict = new Dictionary<int, ProfitEntry>();
    foreach (var sale in querySales)
    {
        foreach (var item in sale.Items)
        {
            var product = repo.Products.FirstOrDefault(p => p.Id == item.ProductId);
            var profit = (item.UnitPrice - (product?.CostPrice ?? 0)) * item.Quantity;
            if (!dict.TryGetValue(item.ProductId, out var entry))
                entry = new ProfitEntry(item.ProductId, product?.Name ?? "#", 0, 0m);
            entry = entry with { Quantity = entry.Quantity + item.Quantity, Profit = entry.Profit + profit };
            dict[item.ProductId] = entry;
        }
    }
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("ProductId,Name,Quantity,Profit");
    foreach (var e in dict.Values)
        sb.AppendLine($"{e.ProductId},{e.Name},{e.Quantity},{e.Profit}");
    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    return Results.File(bytes, "text/csv", $"profit_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

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

record MemberUpdateRequest(string? Name, string? Phone);

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

record UserCreateRequest(string Username, string Password, Role Role);

record UserUpdateRequest(string? Username, string? Password, Role? Role);

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
    public int NextUserId { get; set; } = 2; // starts after seeded admin

    public Repositories()
    {
        // Seed an admin user
        Users.Add(new User { Id = 1, Username = "admin", Password = "admin", Role = Role.Admin });
    }
}

class AuthService
{
    public User? CurrentUser { get; private set; }
    public void SetUser(User? user) => CurrentUser = user;
}

// --------------------- Additional DTO ---------------------
record ProfitEntry(int ProductId, string Name, int Quantity, decimal Profit);

// --------------------- PDF Document ---------------------
class ReceiptDocument : IDocument
{
    private readonly Sale _sale;
    private readonly List<Product> _products;
    public ReceiptDocument(Sale sale, List<Product> products)
    {
        _sale = sale;
        _products = products;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A6);
            page.Margin(20);
            page.Content().Column(col =>
            {
                col.Item().AlignCenter().Text("*** RECEIPT ***").Bold();
                col.Item().Text($"Sale #: {_sale.Id}");
                col.Item().Text($"Date  : {_sale.Timestamp:yyyy-MM-dd HH:mm}");
                col.Item().Text($"Payment: {_sale.PaymentType}");
                col.Item().Text("  ");

                foreach (var item in _sale.Items)
                {
                    var product = _products.FirstOrDefault(p => p.Id == item.ProductId);
                    var name = product?.Name ?? "#";
                    col.Item().Text($"{name} x{item.Quantity} @ {item.UnitPrice:C} = {item.Quantity * item.UnitPrice:C}");
                }
                col.Item().Text("  ");
                col.Item().BorderTop(1).PaddingTop(5).Text($"TOTAL: {_sale.Total:C}").Bold();
                col.Item().AlignCenter().Text("Thank you!");
            });
        });
    }
}