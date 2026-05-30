using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = SD.RoleAdmin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Dashboard()
        {
            // Basic stats
            ViewBag.TotalUsers = await _db.Users.CountAsync(u => u.UserRole == SD.RoleUser);
            ViewBag.TotalOrders = await _db.OrderHeaders
                .CountAsync(o => o.PaymentStatus == SD.PaymentStatusApproved);
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalRevenue = await _db.OrderHeaders
                .Where(o => o.PaymentStatus == SD.PaymentStatusApproved)
                .SumAsync(o => (decimal?)o.OrderTotal) ?? 0m;
            ViewBag.LowStockCount = await _db.Products.CountAsync(p => p.StockQuantity <= SD.LowStockThreshold);
            ViewBag.UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead);

            // Recent orders
            ViewBag.RecentOrders = await _db.OrderHeaders
                .Include(o => o.User)
                .Where(o => o.PaymentStatus == SD.PaymentStatusApproved)
                .OrderByDescending(o => o.Id)
                .Take(5)
                .ToListAsync();

            // Sales chart data (last 7 days)
            var startDate = DateTime.Today.AddDays(-6);
            var salesLabels = new List<string>();
            var salesData = new List<decimal>();
            var ordersData = new List<int>();
            for (var d = startDate; d <= DateTime.Today; d = d.AddDays(1))
            {
                var dayTotal = await _db.OrderHeaders
                    .Where(o => o.OrderDate.Date == d.Date && o.PaymentStatus == SD.PaymentStatusApproved)
                    .SumAsync(o => (decimal?)o.OrderTotal) ?? 0m;
                var dayOrders = await _db.OrderHeaders
                    .Where(o => o.OrderDate.Date == d.Date && o.PaymentStatus == SD.PaymentStatusApproved)
                    .CountAsync();
                salesLabels.Add(d.ToString("dd MMM"));
                salesData.Add(dayTotal);
                ordersData.Add(dayOrders);
            }
            ViewBag.SalesLabels = salesLabels;
            ViewBag.SalesData = salesData;
            ViewBag.OrdersData = ordersData;

            // Sales by category
            var salesByCategory = await _db.OrderDetails
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Include(od => od.OrderHeader)
                .Where(od => od.OrderHeader.PaymentStatus == SD.PaymentStatusApproved)
                .GroupBy(od => od.Product.Category!.Name)
                .Select(g => new { Category = g.Key, Revenue = g.Sum(x => x.Price * x.Count) })
                .ToListAsync();
            ViewBag.SalesByCategoryLabels = salesByCategory.Select(s => s.Category).ToList();
            ViewBag.SalesByCategoryData = salesByCategory.Select(s => s.Revenue).ToList();

            // Top products
            ViewBag.TopProducts = await _db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.OrderHeader)
                .Where(od => od.OrderHeader.PaymentStatus == SD.PaymentStatusApproved)
                .GroupBy(od => new { od.ProductId, od.Product.Title })
                .Select(g => new TopProductRow
                {
                    Title = g.Key.Title,
                    Quantity = g.Sum(x => x.Count),
                    Revenue = g.Sum(x => x.Price * x.Count)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            // Low stock products
            ViewBag.LowStockProducts = await _db.Products
                .Where(p => p.StockQuantity <= SD.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.OrderByDescending(u => u.Id).ToListAsync();
            ViewBag.CurrentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return View(users);
        }

        public async Task<IActionResult> Messages()
        {
            var messages = await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
            return View(messages);
        }

        public async Task<IActionResult> Inventory()
        {
            var products = await _db.Products.Include(p => p.Category).OrderBy(p => p.StockQuantity).ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int productId, int stockQuantity)
        {
            if (stockQuantity < 0)
            {
                TempData["ErrorMessage"] = "Stock quantity cannot be negative.";
                return RedirectToAction(nameof(Inventory));
            }

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Inventory));
            }

            product.StockQuantity = stockQuantity;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Stock updated for \"{product.Title}\".";
            return RedirectToAction(nameof(Inventory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkMessageRead(int messageId)
        {
            var message = await _db.ContactMessages.FindAsync(messageId);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Message not found.";
                return RedirectToAction(nameof(Messages));
            }

            message.IsRead = true;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message marked as read.";
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var message = await _db.ContactMessages.FindAsync(messageId);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Message not found.";
                return RedirectToAction(nameof(Messages));
            }

            _db.ContactMessages.Remove(message);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message deleted.";
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlockUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.UserRole == SD.RoleAdmin)
            {
                TempData["ErrorMessage"] = "User not found or cannot block an admin.";
                return RedirectToAction(nameof(Users));
            }

            user.IsBlocked = !user.IsBlocked;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsBlocked ? "User blocked successfully." : "User unblocked successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user.Id == currentAdminId)
            {
                TempData["ErrorMessage"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Users));
            }

            if (user.UserRole == SD.RoleAdmin)
            {
                var adminCount = await _db.Users.CountAsync(u => u.UserRole == SD.RoleAdmin);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot demote the last admin account.";
                    return RedirectToAction(nameof(Users));
                }

                user.UserRole = SD.RoleUser;
                TempData["SuccessMessage"] = $"{user.Name} is now a regular user.";
            }
            else
            {
                user.UserRole = SD.RoleAdmin;
                TempData["SuccessMessage"] = $"{user.Name} is now an admin.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.UserRole == SD.RoleAdmin)
            {
                TempData["ErrorMessage"] = "User not found or cannot delete an admin.";
                return RedirectToAction(nameof(Users));
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }
    }
}
