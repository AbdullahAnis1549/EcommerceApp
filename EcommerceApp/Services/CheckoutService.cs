using EcommerceApp.Data;
using EcommerceApp.Helpers;
using EcommerceApp.Models;
using EcommerceApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace EcommerceApp.Services
{
    public class CheckoutService
    {
        private readonly ApplicationDbContext _db;
        private readonly StripeSettings _stripeSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static string SessionKey(int userId) => $"Checkout:Pending:{userId}";

        public CheckoutService(
            ApplicationDbContext db,
            IOptions<StripeSettings> stripeOptions,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _stripeSettings = stripeOptions.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public string? ValidateStripeConfigured()
        {
            if (string.IsNullOrWhiteSpace(_stripeSettings.SecretKey) ||
                _stripeSettings.SecretKey.Contains("YOUR_SECRET_KEY", StringComparison.OrdinalIgnoreCase))
            {
                return "Stripe is not configured. Add your test API keys in appsettings.Development.json.";
            }

            if (string.Equals(_stripeSettings.Mode, "Live", StringComparison.OrdinalIgnoreCase))
            {
                return "Stripe is set to Live mode. Ensure this is intentional and secure before processing real payments.";
            }

            return null;
        }

        /// <summary>
        /// Removes unpaid orders left from older checkout flows or abandoned attempts.
        /// </summary>
        public async Task CleanupAbandonedPendingOrdersAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var staleOrders = await _db.OrderHeaders
                .Where(o => o.PaymentStatus == SD.PaymentStatusPending && o.OrderDate < cutoff)
                .ToListAsync(cancellationToken);

            if (staleOrders.Count == 0)
                return;

            var staleIds = staleOrders.Select(o => o.Id).ToList();
            var details = await _db.OrderDetails.Where(d => staleIds.Contains(d.OrderId)).ToListAsync(cancellationToken);

            _db.OrderDetails.RemoveRange(details);
            _db.OrderHeaders.RemoveRange(staleOrders);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<(List<ShoppingCart> Cart, string? Error)> LoadAndValidateCartAsync(int userId, CancellationToken cancellationToken = default)
        {
            var cart = await _db.ShoppingCarts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync(cancellationToken);

            if (cart.Count == 0)
                return (cart, "Your cart is empty.");

            foreach (var item in cart)
            {
                if (item.Product.StockQuantity < item.Count)
                {
                    return (cart, $"\"{item.Product.Title}\" only has {item.Product.StockQuantity} in stock. Please update your cart.");
                }

                item.Price = item.Product.Price;
            }

            return (cart, null);
        }

        public async Task<(string? ClientSecret, string? Error)> CreatePaymentIntentAsync(
            int userId,
            OrderHeader shipping,
            IReadOnlyList<ShoppingCart> cart,
            CancellationToken cancellationToken = default)
        {
            await CleanupAbandonedPendingOrdersAsync(cancellationToken);

            var orderTotal = cart.Sum(c => c.Price * c.Count);
            var currency = _stripeSettings.Currency.ToLowerInvariant();

            try
            {
                var paymentIntent = await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = StripeSettings.ToStripeAmount(orderTotal, currency),
                    Currency = currency,
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() }
                    }
                }, cancellationToken: cancellationToken);

                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                    return (null, "Session is not available. Please try again.");

                session.SetJson(SessionKey(userId), new CheckoutSessionData
                {
                    Name = shipping.Name,
                    PhoneNumber = shipping.PhoneNumber,
                    StreetAddress = shipping.StreetAddress,
                    City = shipping.City,
                    State = shipping.State,
                    PostalCode = shipping.PostalCode,
                    OrderTotal = orderTotal,
                    PaymentIntentId = paymentIntent.Id
                });

                return (paymentIntent.ClientSecret, null);
            }
            catch (StripeException ex)
            {
                return (null, "Payment gateway error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> CompletePaymentAsync(
            int userId,
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return (false, "Session expired. Please start checkout again.");

            var checkout = session.GetJson<CheckoutSessionData>(SessionKey(userId));
            if (checkout == null || string.IsNullOrWhiteSpace(checkout.PaymentIntentId))
                return (false, "Checkout session expired. Please return to the cart and try again.");

            if (!string.Equals(checkout.PaymentIntentId, paymentIntentId, StringComparison.Ordinal))
                return (false, "Payment does not match this checkout session.");

            try
            {
                var paymentIntent = await new PaymentIntentService().GetAsync(paymentIntentId, cancellationToken: cancellationToken);
                if (paymentIntent.Status != "succeeded")
                    return (false, "Payment was not completed. Please try again.");

                if (!paymentIntent.Metadata.TryGetValue("userId", out var metaUserId) ||
                    metaUserId != userId.ToString())
                {
                    return (false, "Payment could not be verified for your account.");
                }

                var (cart, cartError) = await LoadAndValidateCartAsync(userId, cancellationToken);
                if (cartError != null)
                    return (false, cartError);

                var expectedTotal = cart.Sum(c => c.Price * c.Count);
                if (expectedTotal != checkout.OrderTotal)
                {
                    return (false, "Your cart changed during checkout. Please review your order and pay again.");
                }

                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var orderHeader = new OrderHeader
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        ShippingDate = DateTime.MinValue,
                        OrderTotal = checkout.OrderTotal,
                        OrderStatus = SD.StatusApproved,
                        PaymentStatus = SD.PaymentStatusApproved,
                        PaymentIntentId = paymentIntentId,
                        Name = checkout.Name,
                        PhoneNumber = checkout.PhoneNumber,
                        StreetAddress = checkout.StreetAddress,
                        City = checkout.City,
                        State = checkout.State,
                        PostalCode = checkout.PostalCode
                    };

                    _db.OrderHeaders.Add(orderHeader);
                    await _db.SaveChangesAsync(cancellationToken);

                    foreach (var item in cart)
                    {
                        var product = await _db.Products.FirstAsync(p => p.Id == item.ProductId, cancellationToken);
                        if (product.StockQuantity < item.Count)
                            throw new InvalidOperationException($"Insufficient stock for {product.Title}.");

                        product.StockQuantity -= item.Count;
                        _db.Products.Update(product);

                        _db.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = orderHeader.Id,
                            ProductId = item.ProductId,
                            Price = item.Price,
                            Count = item.Count
                        });
                    }

                    _db.ShoppingCarts.RemoveRange(cart);
                    await _db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }

                session.Remove(SessionKey(userId));
                return (true, null);
            }
            catch (StripeException ex)
            {
                return (false, "Payment verification failed: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
