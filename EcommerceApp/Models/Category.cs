using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Category
    {
        // Primary Key for the Category table. 
        // This will be used to link Products to a Category later (One-to-Many Relationship).
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [MaxLength(50)]
        [DisplayName("Category Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage = "Display Order must be between 1 and 100.")]
        public int DisplayOrder { get; set; }

        public string? ImageUrl { get; set; }
    }
}
