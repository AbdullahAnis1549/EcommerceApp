using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;


var builder = WebApplication.CreateBuilder(args);

#region MVC Services
builder.Services.AddControllersWithViews();
#endregion

#region Database (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
#endregion

#region Authentication & Session (Login System)
// ── AUTHENTICATION KYA HAI? ──
// Authentication ka matlab hai "User kon hai?". Hum Cookie Authentication use kar rahe hain.
// Jab user sahi email/password dalta hai, server ek encrypted "Cookie" banata hai aur browser ko de deta hai.
// Agli har request par browser wo cookie wapis bhejta hai, jisse server user ko pehchan leta hai.
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login"; // Agar koi un-authorized page access karega toh yahan redirect hoga
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Agar user admin page par gaya toh yahan bhejenge
            // Do not set ExpireTimeSpan here. By default, cookies will be session cookies
            // and will be removed when the browser is closed unless a persistent cookie
            // is explicitly requested during sign-in (IsPersistent = true).
            options.SlidingExpiration = false;
    });

// Session bhi add kar rakhte hain, shopping cart waghera ke temporary data ke liye kaam aa sakta hai.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
#endregion

#region Stripe Payment
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection(StripeSettings.SectionName));

var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}
#endregion

#region Custom Services (Dependency Injection)

builder.Services.AddHttpContextAccessor();

// Email Service
builder.Services.AddScoped<EmailService>();

// Cloudinary Service (Image Upload)
builder.Services.AddScoped<CloudinaryService>();

// Checkout (Stripe + order creation after payment)
builder.Services.AddScoped<EcommerceApp.Services.CheckoutService>();

#endregion

#region Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#endregion

var app = builder.Build();

// Database seeding removed. If you need to reseed, reintroduce a seeder and call it here.

#region Middleware Pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

#region Security Middleware (IMPORTANT ORDER)
// Middleware ka order bohot zaroori hai!
// Pehle Routing -> Phir Authentication (Who are you?) -> Phir Authorization (What can you do?)
app.UseSession();
app.UseAuthentication(); // 👈 Yeh lazmi hai login system ke liye
app.UseAuthorization();
#endregion

#endregion

#region Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
#endregion

app.Run();