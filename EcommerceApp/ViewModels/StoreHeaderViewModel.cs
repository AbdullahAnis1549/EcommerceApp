using EcommerceApp.Models;

namespace EcommerceApp.ViewModels
{
    public class CategoryNavItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public CategoryNavItem(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    /// <summary>Data for storefront header (categories + optional cart totals).</summary>
    public class StoreHeaderViewModel
    {
        public List<CategoryNavItem> Categories { get; set; } = new();

        public int CartItemCount { get; set; }

        public int WishlistCount { get; set; }

        public decimal CartTotal { get; set; }

        public bool IsAuthenticated { get; set; }

        public string? UserDisplayName { get; set; }
    }
}
