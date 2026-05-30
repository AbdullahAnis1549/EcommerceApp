using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // 👈 Admin protection for Product management
    [Authorize(Roles = SD.RoleAdmin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CloudinaryService _cloudinaryService;

        public ProductController(ApplicationDbContext db, CloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Product
        public IActionResult Index()
        {
            // Eager Loading using .Include() to fetch the associated Category for each Product
            var productList = _db.Products.Include(p => p.Category).ToList();
            return View("~/Views/Admin/Products.cshtml", productList);
        }

        // GET: Product/Upsert
        // We use one action for both Update and Insert (Upsert)
        public IActionResult Upsert(int? id)
        {
            // Create a dropdown list for Categories
            IEnumerable<SelectListItem> CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

            ViewBag.CategoryList = CategoryList;

            if (id == null || id == 0)
            {
                // Create Mode
                return View(new Product());
            }
            else
            {
                // Update Mode
                var productFromDb = _db.Products.Find(id);
                if (productFromDb == null)
                {
                    return NotFound();
                }
                return View(productFromDb);
            }
        }

        // POST: Product/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Product obj, IFormFile? file)
        {
            if (obj.ListPrice.HasValue && obj.ListPrice <= obj.Price)
            {
                ModelState.AddModelError(nameof(obj.ListPrice),
                    "Original price must be higher than the sale price.");
            }

            if (obj.IsDeal && !obj.ListPrice.HasValue)
            {
                ModelState.AddModelError(nameof(obj.ListPrice),
                    "Deal products need an original price higher than the sale price.");
            }

            if (ModelState.IsValid)
            {
                // Handle Image Upload using Cloudinary
                if (file != null)
                {
                    string imageUrl = await _cloudinaryService.UploadImageAsync(file, "ecommerce-products");
                    obj.ImageUrl = imageUrl;
                }
                // no spotlight logic required

                if (obj.Id == 0)
                {
                    _db.Products.Add(obj);
                    TempData["success"] = "Product created successfully";
                }
                else
                {
                    // If a new file wasn't uploaded during edit, keep the old image URL
                    if (file == null && string.IsNullOrEmpty(obj.ImageUrl))
                    {
                        var productFromDb = _db.Products.AsNoTracking().FirstOrDefault(p => p.Id == obj.Id);
                        if (productFromDb != null)
                        {
                            obj.ImageUrl = productFromDb.ImageUrl;
                        }
                    }

                    _db.Products.Update(obj);
                    TempData["success"] = "Product updated successfully";
                }

                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Re-populate Dropdown if model state is invalid
            IEnumerable<SelectListItem> CategoryList = _db.Categories.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
            ViewBag.CategoryList = CategoryList;

            return View(obj);
        }

        // GET: Product/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            // Include Category for display purposes
            var productFromDb = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            var obj = _db.Products.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            // Optional: You could also call _cloudinaryService.DeleteImageAsync(obj.ImageUrl) here to save space

            _db.Products.Remove(obj);
            await _db.SaveChangesAsync();
            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }
    }
}
