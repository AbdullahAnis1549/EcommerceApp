using EcommerceApp.Data;
using EcommerceApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.ViewComponents
{
    public class StoreHeaderViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public StoreHeaderViewComponent(ApplicationDbContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new StoreHeaderViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                Categories = await _db.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new CategoryNavItem(c.Id, c.Name))
                    .ToListAsync(),
            };

            if (vm.IsAuthenticated && User.Identity != null)
            {
                vm.UserDisplayName = User.Identity.Name;
                var userIdStr = UserClaimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    vm.WishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == userId);

                    var lines = await _db.ShoppingCarts
                        .Include(sc => sc.Product)
                        .Where(sc => sc.UserId == userId)
                        .ToListAsync();

                    vm.CartItemCount = lines.Sum(l => l.Count);
                    vm.CartTotal = lines.Sum(l => l.Product.Price * l.Count);
                }
            }

            return View(vm);
        }
    }
}
