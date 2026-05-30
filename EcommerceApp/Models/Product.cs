using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000, ErrorMessage = "Sale price must be greater than zero")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Sale price (customer pays)")]
        public decimal Price { get; set; }

        /// <summary>Original price before discount. Must be greater than Price when set.</summary>
        [Range(0.01, 100000, ErrorMessage = "Original price must be greater than zero")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Original price (optional, for deals)")]
        public decimal? ListPrice { get; set; }

        [NotMapped]
        public bool HasActiveDiscount => ListPrice.HasValue && ListPrice > Price;

        [NotMapped]
        public int DiscountPercent => HasActiveDiscount
            ? (int)Math.Round((1 - (double)(Price / ListPrice!.Value)) * 100, MidpointRounding.AwayFromZero)
            : 0;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; } = 100;

        // Foreign Key linking to Category
        [Required(ErrorMessage = "Please select a Category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Show on Deals page (requires original price > sale price)")]
        public bool IsDeal { get; set; }

        [Display(Name = "Show on New Arrivals page")]
        public bool IsNewArrival { get; set; }

        [Display(Name = "Show in Best Sellers (home)")]
        public bool IsBestSeller { get; set; }
    }
}
