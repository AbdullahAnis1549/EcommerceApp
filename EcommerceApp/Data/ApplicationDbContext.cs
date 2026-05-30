using EcommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // ── DbSets ──
        // Har DbSet ek database TABLE represent karta hai.
        // EF Core in properties ko use karke tables create karta hai.
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<AboutPage> AboutPages { get; set; }

        // ── OnModelCreating ──
        // Yeh method tab call hota hai jab EF Core database ka model build karta hai.
        // Is mein hum FLUENT API use karke relationships aur configurations define karte hain.
        //
        // FLUENT API vs DATA ANNOTATIONS:
        // - Data Annotations: Model ke upar [Key], [Required], [ForeignKey] etc. attributes lagate hain
        //   → Simple aur quick hai, lekin limited control milta hai
        // - Fluent API: Yahan OnModelCreating mein likhte hain
        //   → Zyada powerful hai, complex relationships (One-to-One, Many-to-Many) ke liye zaroori hai
        //   → Keeps models clean — sab configuration ek jagah hoti hai
        //
        // Best Practice: Dono ko combine karo! Simple cheezein (Required, MaxLength) → Annotations
        //                Complex relationships → Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── One-to-One Relationship: User <-> UserProfile ──
            //
            // .HasOne(u => u.UserProfile)  → "User" ke paas EK UserProfile hai
            // .WithOne(up => up.User)      → "UserProfile" ke paas bhi EK hi User hai
            // .HasForeignKey<UserProfile>(up => up.UserId) → Foreign Key "UserProfile" table mein hai
            //
            // Is configuration ke baghair EF Core confuse ho sakta hai:
            //   - Kya yeh One-to-One hai ya One-to-Many?
            //   - Foreign Key kis table mein hai?
            // Fluent API se hum yeh sab EXPLICITLY bata dete hain.
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId);

            // ── Product Price Precision ──
            // Decimal fields ke liye precision define karna zaroori hai
            // warna SQL Server default precision use karega aur values truncate ho sakti hain.
            // HasPrecision(18, 2) = 18 total digits, 2 decimal places (e.g., 9999999999999999.99)
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.ListPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderHeader>()
                .Property(o => o.OrderTotal)
                .HasPrecision(18, 2);
            
            modelBuilder.Entity<OrderDetail>()
                .Property(o => o.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();
        }
    }
}
