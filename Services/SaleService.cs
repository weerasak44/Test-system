using POS_System.Data;
using POS_System.Models;
using Microsoft.EntityFrameworkCore;

namespace POS_System.Services
{
    public class SaleService
    {
        private readonly POSDbContext _context;
        private readonly ProductService _productService;

        public SaleService(POSDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public SaleService()
        {
            _context = new POSDbContext();
            _productService = new ProductService(_context);
        }

        public async Task<Sale> CreateSaleAsync(int? customerId, PriceLevel priceLevel, PaymentMethod paymentMethod, List<SaleItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = new Sale
                {
                    SaleNumber = await GenerateSaleNumberAsync(),
                    CustomerId = customerId,
                    UserId = AuthService.CurrentUser?.UserId ?? 1,
                    SaleDate = DateTime.Now,
                    PriceLevel = priceLevel,
                    PaymentMethod = paymentMethod,
                    Status = SaleStatus.Pending
                };

                // Calculate totals
                decimal subTotal = 0;
                foreach (var item in items)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        item.UnitPrice = _productService.GetPriceByLevel(product, priceLevel);
                        item.TotalPrice = item.Quantity * item.UnitPrice;
                        subTotal += item.TotalPrice;
                    }
                }

                sale.SubTotal = subTotal;
                sale.TotalAmount = subTotal - sale.DiscountAmount;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Add sale items
                foreach (var item in items)
                {
                    item.SaleId = sale.SaleId;
                    _context.SaleItems.Add(item);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return sale;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CompleteSaleAsync(int saleId, decimal paidAmount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                    .FirstOrDefaultAsync(s => s.SaleId == saleId);

                if (sale == null || sale.Status != SaleStatus.Pending)
                    return false;

                sale.PaidAmount = paidAmount;
                sale.ChangeAmount = paidAmount - sale.TotalAmount;
                sale.Status = SaleStatus.Completed;

                // Update stock quantities
                foreach (var item in sale.SaleItems)
                {
                    await _productService.UpdateStockAsync(item.ProductId, -(int)item.Quantity);
                }

                // Update customer debt if credit sale
                if (sale.PaymentMethod == PaymentMethod.Credit && sale.CustomerId.HasValue)
                {
                    var customer = await _context.Customers.FindAsync(sale.CustomerId.Value);
                    if (customer != null)
                    {
                        customer.CurrentDebt += sale.TotalAmount;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CancelSaleAsync(int saleId)
        {
            try
            {
                var sale = await _context.Sales.FindAsync(saleId);
                if (sale != null && sale.Status == SaleStatus.Pending)
                {
                    sale.Status = SaleStatus.Cancelled;
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

        public async Task<List<Sale>> GetSalesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .Where(s => s.SaleDate >= fromDate && s.SaleDate <= toDate && s.Status == SaleStatus.Completed)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<Sale?> GetSaleByIdAsync(int saleId)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);
        }

        private async Task<string> GenerateSaleNumberAsync()
        {
            var today = DateTime.Today;
            var todayString = today.ToString("yyyyMMdd");
            
            var lastSale = await _context.Sales
                .Where(s => s.SaleDate.Date == today)
                .OrderByDescending(s => s.SaleNumber)
                .FirstOrDefaultAsync();

            if (lastSale != null && lastSale.SaleNumber.StartsWith($"S{todayString}"))
            {
                var lastNumber = int.Parse(lastSale.SaleNumber.Substring(9));
                return $"S{todayString}{(lastNumber + 1):000}";
            }

            return $"S{todayString}001";
        }
    }
}