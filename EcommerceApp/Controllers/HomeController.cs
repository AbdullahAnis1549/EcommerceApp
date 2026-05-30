using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) => _db = db;

        public IActionResult Index()
        {
            ViewBag.Categories = _db.Categories.OrderBy(c => c.DisplayOrder).ToList();
            ViewBag.Banners = _db.Banners.Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToList();

            var bestSellers = _db.Products.Include(p => p.Category)
                .Where(p => p.IsBestSeller)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();

            if (!bestSellers.Any())
            {
                bestSellers = _db.Products.Include(p => p.Category)
                    .OrderByDescending(p => p.Id)
                    .Skip(1)
                    .Take(8)
                    .ToList();
            }

            ViewBag.BestSellers = bestSellers;

            return View();
        }

        public IActionResult Deals()
        {
            var products = _db.Products.Include(p => p.Category)
                .Where(p => p.IsDeal && p.ListPrice != null && p.ListPrice > p.Price)
                .OrderByDescending(p => p.ListPrice - p.Price)
                .ToList();
            return View(products);
        }

        public IActionResult NewArrivals()
        {
            var products = _db.Products.Include(p => p.Category)
                .Where(p => p.IsNewArrival)
                .OrderByDescending(p => p.Id)
                .ToList();
            return View(products);
        }

        public IActionResult Shop(int? categoryId, string? search, string? sort, decimal? minPrice, decimal? maxPrice)
        {
            ViewBag.Categories = _db.Categories.OrderBy(c => c.DisplayOrder).ToList();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            IQueryable<Product> query = _db.Products.Include(p => p.Category);

            if (categoryId != null && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.SelectedCategoryName = _db.Categories.Find(categoryId)?.Name ?? "All Products";
            }
            else
                ViewBag.SelectedCategoryName = "All Products";

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.Id)
            };

            return View(query.ToList());
        }

        public IActionResult Search(string? q) => RedirectToAction(nameof(Shop), new { search = q });

        public IActionResult Details(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var product = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            ViewBag.RelatedProducts = _db.Products.Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id).Take(4).ToList();

            if (User.Identity?.IsAuthenticated == true)
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                ViewBag.InWishlist = _db.Wishlists.Any(w => w.UserId == userId && w.ProductId == product.Id);
            }

            return View(new ShoppingCart { Count = 1, ProductId = product.Id, Product = product });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Details([Bind("ProductId,Count")] ShoppingCart shoppingCart)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var product = _db.Products.Find(shoppingCart.ProductId);

            if (product == null) return NotFound();
            if (product.StockQuantity < shoppingCart.Count)
            {
                TempData["ErrorMessage"] = $"Only {product.StockQuantity} items in stock.";
                return RedirectToAction(nameof(Details), new { id = shoppingCart.ProductId });
            }

            shoppingCart.UserId = userId;
            var cartFromDb = _db.ShoppingCarts.FirstOrDefault(u => u.UserId == userId && u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                if (cartFromDb.Count + shoppingCart.Count > product.StockQuantity)
                {
                    TempData["ErrorMessage"] = $"Cannot add more than {product.StockQuantity} items.";
                    return RedirectToAction(nameof(Details), new { id = shoppingCart.ProductId });
                }
                cartFromDb.Count += shoppingCart.Count;
                _db.ShoppingCarts.Update(cartFromDb);
            }
            else
                _db.ShoppingCarts.Add(shoppingCart);

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Added to cart!";
            return RedirectToAction(nameof(Details), new { id = shoppingCart.ProductId });
        }

        public async Task<IActionResult> About()
        {
            var page = await _db.AboutPages.OrderBy(p => p.Id).FirstOrDefaultAsync();
            return View(page);
        }
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return View();
            }

            _db.ContactMessages.Add(new ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Message = message.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! We received your message and will reply soon.";
            return View();
        }

        public IActionResult Terms() => View();
        public IActionResult ReturnPolicy() => View();
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
