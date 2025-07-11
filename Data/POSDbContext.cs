using Microsoft.EntityFrameworkCore;
using POS_System.Models;

namespace POS_System.Data
{
    public class POSDbContext : DbContext
    {
        public POSDbContext(DbContextOptions<POSDbContext> options) : base(options)
        {
        }

        public POSDbContext() : base()
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\POSDatabase.mdf;Integrated Security=True;Connect Timeout=30");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(e => e.ProductCode).IsUnique();
                entity.Property(e => e.CostPrice).HasPrecision(18, 2);
                entity.Property(e => e.NormalPrice).HasPrecision(18, 2);
                entity.Property(e => e.EmployeePrice).HasPrecision(18, 2);
                entity.Property(e => e.WholesalePrice).HasPrecision(18, 2);
            });

            // Configure Customer
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.CustomerCode).IsUnique();
                entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
                entity.Property(e => e.CurrentDebt).HasPrecision(18, 2);
            });

            // Configure Sale
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasIndex(e => e.SaleNumber).IsUnique();
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
                entity.Property(e => e.ChangeAmount).HasPrecision(18, 2);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Sales)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure SaleItem
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(18, 3);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(d => d.Sale)
                    .WithMany(p => p.SaleItems)
                    .HasForeignKey(d => d.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.SaleItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(e => e.PaymentNumber).IsUnique();
                entity.Property(e => e.PaymentAmount).HasPrecision(18, 2);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Admin User (using fixed date to avoid seeding issues)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$1UOKLeDcKZF9j5YdVDMBqOVZ8h8D5xGxPxvGe7I3ZGLr2j7D4UlMu", // admin123 hashed
                    FullName = "ผู้ดูแลระบบ",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1)
                }
            );

            // Seed Sample Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    ProductCode = "P001",
                    ProductName = "ข้าวสาร 5 กิโลกรัม",
                    Description = "ข้าวสารคุณภาพดี",
                    Unit = "ถุง",
                    CostPrice = 150.00m,
                    NormalPrice = 200.00m,
                    EmployeePrice = 180.00m,
                    WholesalePrice = 170.00m,
                    StockQuantity = 100,
                    MinStockLevel = 10,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1)
                },
                new Product
                {
                    ProductId = 2,
                    ProductCode = "P002",
                    ProductName = "น้ำมันพืช 1 ลิตร",
                    Description = "น้ำมันพืชสำหรับทำอาหาร",
                    Unit = "ขวด",
                    CostPrice = 35.00m,
                    NormalPrice = 50.00m,
                    EmployeePrice = 45.00m,
                    WholesalePrice = 40.00m,
                    StockQuantity = 200,
                    MinStockLevel = 20,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1)
                }
            );

            // Seed Sample Customer
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    CustomerId = 1,
                    CustomerCode = "C001",
                    CustomerName = "ลูกค้าทั่วไป",
                    Phone = "",
                    Email = "",
                    Address = "",
                    CreditLimit = 0,
                    CurrentDebt = 0,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1)
                }
            );
        }
    }
}