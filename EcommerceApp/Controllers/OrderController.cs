using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderController(ApplicationDbContext db) => _db = db;

        public IActionResult Index()
        {
            IQueryable<OrderHeader> query = _db.OrderHeaders
                .Include(u => u.User)
                .Where(o => o.PaymentStatus == SD.PaymentStatusApproved);

            if (!User.IsInRole(SD.RoleAdmin))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                query = query.Where(o => o.UserId == userId);
            }

            var orders = query.OrderByDescending(o => o.Id).ToList();

            if (User.IsInRole(SD.RoleAdmin))
                return View("~/Views/Admin/Orders.cshtml", orders);

            return View(orders);
        }

        public IActionResult Details(int orderId)
        {
            var orderHeader = _db.OrderHeaders.Include(u => u.User).FirstOrDefault(o => o.Id == orderId);
            if (orderHeader == null) return NotFound();

            if (!User.IsInRole(SD.RoleAdmin))
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (orderHeader.UserId != userId) return Unauthorized();
            }

            ViewBag.OrderDetails = _db.OrderDetails.Include(o => o.Product).Where(o => o.OrderId == orderId).ToList();
            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.RoleAdmin)]
        public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus)
        {
            var order = await _db.OrderHeaders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (!SD.AllowedOrderStatuses.Contains(orderStatus))
            {
                TempData["error"] = "Invalid order status.";
                return RedirectToAction(nameof(Details), new { orderId });
            }

            order.OrderStatus = orderStatus;
            if (orderStatus == SD.StatusShipped)
                order.ShippingDate = DateTime.Now;

            await _db.SaveChangesAsync();
            TempData["success"] = "Order status updated.";
            return RedirectToAction(nameof(Details), new { orderId });
        }
    }
}
