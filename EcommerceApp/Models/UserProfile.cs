using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// UserProfile model — User ki extra personal information store karta hai.
    /// 
    /// ── ONE-TO-ONE RELATIONSHIP ──
    /// Har User ka sirf EK UserProfile hoga, aur har UserProfile sirf EK User se linked hoga.
    /// 
    /// Relationship diagram:
    ///   User (1) ──────── (1) UserProfile
    ///    Id  ◄──FK────────── UserId
    /// 
    /// "UserId" yahan FOREIGN KEY hai jo "User" table ke "Id" (Primary Key) ko reference karta hai.
    /// EF Core is Foreign Key ko use karke dono tables ko JOIN karta hai.
    /// </summary>
    public class UserProfile
    {
        // Primary Key for UserProfile table
        [Key]
        public int Id { get; set; }

        // ── Address Fields ──

        [Required(ErrorMessage = "Address is required")]
        [StringLength(250)]
        [Display(Name = "Full Address")]
        public string FullAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal Code is required")]
        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        // ── Optional Personal Info ──

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        [Display(Name = "About Me")]
        public string? Bio { get; set; }

        // ── FOREIGN KEY ──
        // Yeh field "User" table ke "Id" column ko reference karta hai.
        // Is se EF Core ko pata chal jata hai ke yeh UserProfile kis User ka hai.
        // "Required" attribute lagaya hai kyunke har UserProfile LAZMI kisi User se linked hona chahiye.
        [Required]
        public int UserId { get; set; }

        // ── NAVIGATION PROPERTY ──
        // Yeh actual "User" object hai jo is UserProfile se linked hai.
        // Jab hum .Include(up => up.User) use karenge toh yeh property automatically populate ho jayegi.
        // [ForeignKey("UserId")] EF Core ko explicitly bata deta hai ke "UserId" field hi Foreign Key hai.
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
