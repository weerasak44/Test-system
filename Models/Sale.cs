using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_System.Models
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string SaleNumber { get; set; } = string.Empty;
        
        public int? CustomerId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Now;
        
        [Required]
        public PriceLevel PriceLevel { get; set; }
        
        [Required]
        public decimal SubTotal { get; set; }
        
        public decimal DiscountAmount { get; set; } = 0;
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        public decimal PaidAmount { get; set; } = 0;
        
        public decimal ChangeAmount { get; set; } = 0;
        
        [Required]
        public SaleStatus Status { get; set; }
        
        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
    
    public enum PriceLevel
    {
        Normal = 1,
        Employee = 2,
        Wholesale = 3
    }
    
    public enum PaymentMethod
    {
        Cash = 1,
        Transfer = 2,
        Credit = 3
    }
    
    public enum SaleStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3
    }
}