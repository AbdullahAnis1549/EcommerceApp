using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceApp.ViewModels
{
    public class ShoppingCartVM
    {
        [ValidateNever]
        public IEnumerable<ShoppingCart> ListCart { get; set; } = new List<ShoppingCart>();

        public OrderHeader OrderHeader { get; set; } = new OrderHeader();
    }
}
