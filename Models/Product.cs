using System.ComponentModel.DataAnnotations;

namespace POS_System.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;
        
        [Required]
        public decimal CostPrice { get; set; }
        
        [Required]
        public decimal NormalPrice { get; set; }
        
        [Required]
        public decimal EmployeePrice { get; set; }
        
        [Required]
        public decimal WholesalePrice { get; set; }
        
        public int StockQuantity { get; set; } = 0;
        
        public int MinStockLevel { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}