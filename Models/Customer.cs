using System.ComponentModel.DataAnnotations;

namespace POS_System.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string CustomerCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;
        
        public decimal CreditLimit { get; set; } = 0;
        
        public decimal CurrentDebt { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}