using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;

        public WishlistController(ApplicationDbContext db) => _db = db;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public IActionResult Index()
        {
            var items = _db.Wishlists
                .Include(w => w.Product).ThenInclude(p => p!.Category)
                .Where(w => w.UserId == GetUserId())
                .ToList();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            int userId = GetUserId();
            var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                TempData["SuccessMessage"] = "Removed from wishlist.";
            }
            else
            {
                _db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
                TempData["SuccessMessage"] = "Added to wishlist.";
            }

            await _db.SaveChangesAsync();
            return Redirect(Request.Headers["Referer"].ToString() ?? Url.Action("Index", "Home")!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _db.Wishlists.FirstOrDefaultAsync(w => w.Id == id && w.UserId == GetUserId());
            if (item != null)
            {
                _db.Wishlists.Remove(item);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Removed from wishlist.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
