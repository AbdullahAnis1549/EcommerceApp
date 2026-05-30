using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using EcommerceApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CheckoutService _checkout;
        private readonly StripeSettings _stripeSettings;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; } = null!;

        public CartController(
            ApplicationDbContext db,
            CheckoutService checkout,
            IOptions<StripeSettings> stripeOptions)
        {
            _db = db;
            _checkout = checkout;
            _stripeSettings = stripeOptions.Value;
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int userId) ? userId : null;
        }

        private ShoppingCart? GetUserCartItem(int cartId, int userId) =>
            _db.ShoppingCarts.Include(c => c.Product).FirstOrDefault(c => c.Id == cartId && c.UserId == userId);

        public IActionResult Index()
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _db.ShoppingCarts.Include(u => u.Product).Where(u => u.UserId == userId.Value),
                OrderHeader = new OrderHeader()
            };

            foreach (var cart in ShoppingCartVM.ListCart)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _db.ShoppingCarts.Include(u => u.Product).Where(u => u.UserId == userId.Value),
                OrderHeader = new OrderHeader()
            };

            if (!ShoppingCartVM.ListCart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Add products before checkout.";
                return RedirectToAction(nameof(Index));
            }

            var applicationUser = _db.Users.Include(u => u.UserProfile).FirstOrDefault(u => u.Id == userId.Value);
            if (applicationUser != null)
            {
                ShoppingCartVM.OrderHeader.Name = applicationUser.Name;
                ShoppingCartVM.OrderHeader.PhoneNumber = applicationUser.Phone;

                if (applicationUser.UserProfile != null)
                {
                    ShoppingCartVM.OrderHeader.StreetAddress = applicationUser.UserProfile.FullAddress;
                    ShoppingCartVM.OrderHeader.City = applicationUser.UserProfile.City;
                    ShoppingCartVM.OrderHeader.State = applicationUser.UserProfile.State;
                    ShoppingCartVM.OrderHeader.PostalCode = applicationUser.UserProfile.PostalCode;
                }
            }

            foreach (var cart in ShoppingCartVM.ListCart)
            {
                cart.Price = cart.Product.Price;
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            ViewBag.StripePublishableKey = _stripeSettings.PublishableKey;
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(CancellationToken cancellationToken)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var stripeError = _checkout.ValidateStripeConfigured();
            if (stripeError != null)
                return Json(new { error = stripeError });

            var (cart, cartError) = await _checkout.LoadAndValidateCartAsync(userId.Value, cancellationToken);
            if (cartError != null)
                return Json(new { error = cartError });

            ModelState.Remove(nameof(ShoppingCartVM.ListCart));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();
                return Json(new { error = errors.FirstOrDefault() ?? "Please complete all shipping fields." });
            }

            var (clientSecret, error) = await _checkout.CreatePaymentIntentAsync(
                userId.Value,
                ShoppingCartVM.OrderHeader,
                cart,
                cancellationToken);

            if (error != null)
                return Json(new { error });

            return Json(new { clientSecret });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment([FromForm] string paymentIntentId, CancellationToken cancellationToken)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(paymentIntentId))
                return Json(new { error = "Missing payment information." });

            var stripeError = _checkout.ValidateStripeConfigured();
            if (stripeError != null)
                return Json(new { error = stripeError });

            var (success, error) = await _checkout.CompletePaymentAsync(userId.Value, paymentIntentId, cancellationToken);
            if (!success)
                return Json(new { error = error ?? "Checkout could not be completed." });

            return Json(new { redirectUrl = Url.Action("Index", "Home") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Plus(int cartId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartFromDb = GetUserCartItem(cartId, userId.Value);
            if (cartFromDb == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            if (cartFromDb.Count + 1 > cartFromDb.Product.StockQuantity)
            {
                TempData["ErrorMessage"] = $"Only {cartFromDb.Product.StockQuantity} items available in stock.";
                return RedirectToAction(nameof(Index));
            }

            cartFromDb.Count += 1;
            _db.ShoppingCarts.Update(cartFromDb);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Minus(int cartId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartFromDb = GetUserCartItem(cartId, userId.Value);
            if (cartFromDb == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            if (cartFromDb.Count <= 1)
                _db.ShoppingCarts.Remove(cartFromDb);
            else
            {
                cartFromDb.Count -= 1;
                _db.ShoppingCarts.Update(cartFromDb);
            }

            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int cartId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var cartFromDb = GetUserCartItem(cartId, userId.Value);
            if (cartFromDb == null)
            {
                TempData["ErrorMessage"] = "Cart item not found.";
                return RedirectToAction(nameof(Index));
            }

            _db.ShoppingCarts.Remove(cartFromDb);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
