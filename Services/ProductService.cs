using POS_System.Data;
using POS_System.Models;
using Microsoft.EntityFrameworkCore;

namespace POS_System.Services
{
    public class ProductService
    {
        private readonly POSDbContext _context;

        public ProductService(POSDbContext context)
        {
            _context = context;
        }

        public ProductService()
        {
            _context = new POSDbContext();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
        }

        public async Task<Product?> GetProductByCodeAsync(string productCode)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive);
        }

        public async Task<List<Product>> SearchProductsAsync(string searchText)
        {
            return await _context.Products
                .Where(p => p.IsActive && 
                    (p.ProductCode.Contains(searchText) || p.ProductName.Contains(searchText)))
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                // Generate new product code if not provided
                if (string.IsNullOrEmpty(product.ProductCode))
                {
                    product.ProductCode = await GenerateProductCodeAsync();
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                product.UpdatedDate = DateTime.Now;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.IsActive = false;
                    product.UpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.StockQuantity += quantity;
                    product.UpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public decimal GetPriceByLevel(Product product, PriceLevel priceLevel)
        {
            return priceLevel switch
            {
                PriceLevel.Normal => product.NormalPrice,
                PriceLevel.Employee => product.EmployeePrice,
                PriceLevel.Wholesale => product.WholesalePrice,
                _ => product.NormalPrice
            };
        }

        private async Task<string> GenerateProductCodeAsync()
        {
            var lastProduct = await _context.Products
                .OrderByDescending(p => p.ProductCode)
                .FirstOrDefaultAsync();

            if (lastProduct != null && lastProduct.ProductCode.StartsWith("P"))
            {
                var lastNumber = int.Parse(lastProduct.ProductCode.Substring(1));
                return $"P{(lastNumber + 1):000}";
            }

            return "P001";
        }
    }
}