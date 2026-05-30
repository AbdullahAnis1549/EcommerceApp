using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = SD.RoleAdmin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CloudinaryService _cloudinary;

        public CategoryController(ApplicationDbContext db, CloudinaryService cloudinary)
        {
            _db = db;
            _cloudinary = cloudinary;
        }

        public IActionResult Index() => View("~/Views/Admin/Categories.cshtml", _db.Categories.OrderBy(c => c.DisplayOrder).ToList());

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category obj, IFormFile? file)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
                ModelState.AddModelError("Name", "Display order cannot match the name.");

            if (!ModelState.IsValid) return View(obj);

            if (file != null)
                obj.ImageUrl = await _cloudinary.UploadImageAsync(file, "ecommerce-categories");

            _db.Categories.Add(obj);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var cat = _db.Categories.Find(id);
            return cat == null ? NotFound() : View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category obj, IFormFile? file)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
                ModelState.AddModelError("Name", "Display order cannot match the name.");

            if (!ModelState.IsValid) return View(obj);

            if (file != null)
                obj.ImageUrl = await _cloudinary.UploadImageAsync(file, "ecommerce-categories");
            else
            {
                var existing = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == obj.Id);
                if (existing != null) obj.ImageUrl = existing.ImageUrl;
            }

            _db.Categories.Update(obj);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            var cat = _db.Categories.Find(id);
            return cat == null ? NotFound() : View(cat);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _db.Categories.Find(id);
            if (obj == null) return NotFound();

            if (_db.Products.Any(p => p.CategoryId == obj.Id))
            {
                TempData["error"] = "Cannot delete this category because it has products assigned. Reassign or delete those products first.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
