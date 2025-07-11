using POS_System.Data;
using POS_System.Models;
using Microsoft.EntityFrameworkCore;

namespace POS_System.Services
{
    public class CustomerService
    {
        private readonly POSDbContext _context;

        public CustomerService(POSDbContext context)
        {
            _context = context;
        }

        public CustomerService()
        {
            _context = new POSDbContext();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);
        }

        public async Task<Customer?> GetCustomerByCodeAsync(string customerCode)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerCode == customerCode && c.IsActive);
        }

        public async Task<List<Customer>> SearchCustomersAsync(string searchText)
        {
            return await _context.Customers
                .Where(c => c.IsActive && 
                    (c.CustomerCode.Contains(searchText) || c.CustomerName.Contains(searchText)))
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            try
            {
                if (string.IsNullOrEmpty(customer.CustomerCode))
                {
                    customer.CustomerCode = await GenerateCustomerCodeAsync();
                }

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                customer.UpdatedDate = DateTime.Now;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    customer.IsActive = false;
                    customer.UpdatedDate = DateTime.Now;
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

        public async Task<List<Customer>> GetCustomersWithDebtAsync()
        {
            return await _context.Customers
                .Where(c => c.IsActive && c.CurrentDebt > 0)
                .OrderByDescending(c => c.CurrentDebt)
                .ToListAsync();
        }

        public async Task<List<Sale>> GetCustomerSalesHistoryAsync(int customerId)
        {
            return await _context.Sales
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Product)
                .Where(s => s.CustomerId == customerId && s.Status == SaleStatus.Completed)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<bool> MakePaymentAsync(int customerId, decimal paymentAmount, PaymentMethod paymentMethod, string remarks = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null || customer.CurrentDebt < paymentAmount)
                    return false;

                // Create payment record
                var payment = new Payment
                {
                    PaymentNumber = await GeneratePaymentNumberAsync(),
                    CustomerId = customerId,
                    UserId = AuthService.CurrentUser?.UserId ?? 1,
                    PaymentDate = DateTime.Now,
                    PaymentAmount = paymentAmount,
                    PaymentMethod = paymentMethod,
                    Remarks = remarks
                };

                _context.Payments.Add(payment);

                // Update customer debt
                customer.CurrentDebt -= paymentAmount;

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

        private async Task<string> GenerateCustomerCodeAsync()
        {
            var lastCustomer = await _context.Customers
                .OrderByDescending(c => c.CustomerCode)
                .FirstOrDefaultAsync();

            if (lastCustomer != null && lastCustomer.CustomerCode.StartsWith("C"))
            {
                var lastNumber = int.Parse(lastCustomer.CustomerCode.Substring(1));
                return $"C{(lastNumber + 1):000}";
            }

            return "C001";
        }

        private async Task<string> GeneratePaymentNumberAsync()
        {
            var today = DateTime.Today;
            var todayString = today.ToString("yyyyMMdd");
            
            var lastPayment = await _context.Payments
                .Where(p => p.PaymentDate.Date == today)
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();

            if (lastPayment != null && lastPayment.PaymentNumber.StartsWith($"P{todayString}"))
            {
                var lastNumber = int.Parse(lastPayment.PaymentNumber.Substring(9));
                return $"P{todayString}{(lastNumber + 1):000}";
            }

            return $"P{todayString}001";
        }
    }
}