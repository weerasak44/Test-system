using POS_System.Data;
using POS_System.Models;
using Microsoft.EntityFrameworkCore;

namespace POS_System.Services
{
    public class AuthService
    {
        private readonly POSDbContext _context;

        public AuthService(POSDbContext context)
        {
            _context = context;
        }

        public AuthService()
        {
            _context = new POSDbContext();
        }

        public static User? CurrentUser { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    CurrentUser = user;
                    user.LastLoginDate = DateTime.Now;
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

        public void Logout()
        {
            CurrentUser = null;
        }

        public bool IsLoggedIn()
        {
            return CurrentUser != null;
        }

        public bool IsAdmin()
        {
            return CurrentUser?.Role == UserRole.Admin;
        }

        public bool IsCashier()
        {
            return CurrentUser?.Role == UserRole.Cashier;
        }
    }
}