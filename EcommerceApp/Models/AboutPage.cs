using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class AboutPage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "About Us";

        [MaxLength(500)]
        public string? Subtitle { get; set; }

        [Required]
        public string Body { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
