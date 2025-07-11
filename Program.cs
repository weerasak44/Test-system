using Microsoft.EntityFrameworkCore;
using POS_System.Data;
using POS_System.Forms;

namespace POS_System
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize database
            using (var context = new POSDbContext())
            {
                try
                {
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล: {ex.Message}", 
                        "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Application.Run(new LoginForm());
        }
    }
}