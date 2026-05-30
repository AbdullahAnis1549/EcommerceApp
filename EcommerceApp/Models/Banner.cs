using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Subtitle { get; set; }

        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
