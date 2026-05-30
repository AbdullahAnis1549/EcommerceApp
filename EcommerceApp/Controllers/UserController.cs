using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Home()
        {
            if (User.IsInRole(SD.RoleAdmin))
                return RedirectToAction("Dashboard", "Admin");

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            ViewBag.CartCount = await _db.ShoppingCarts.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Count) ?? 0;
            ViewBag.WishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == userId);
            ViewBag.OrderCount = await _db.OrderHeaders.CountAsync(o =>
                o.UserId == userId && o.PaymentStatus == SD.PaymentStatusApproved);

            return View();
        }
    }
}
