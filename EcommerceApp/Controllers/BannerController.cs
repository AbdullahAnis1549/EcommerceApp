using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = SD.RoleAdmin)]
    public class BannerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CloudinaryService _cloudinary;

        public BannerController(ApplicationDbContext db, CloudinaryService cloudinary)
        {
            _db = db;
            _cloudinary = cloudinary;
        }

        public IActionResult Index() => View("~/Views/Admin/Banners.cshtml", _db.Banners.OrderBy(b => b.DisplayOrder).ToList());

        public IActionResult Upsert(int? id) => View(id == null || id == 0 ? new Banner() : _db.Banners.Find(id) ?? new Banner());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Banner obj, IFormFile? file)
        {
            if (!ModelState.IsValid) return View(obj);

            if (file != null)
                obj.ImageUrl = await _cloudinary.UploadImageAsync(file, "ecommerce-banners");
            else if (obj.Id != 0)
            {
                var existing = await _db.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == obj.Id);
                if (existing != null) obj.ImageUrl = existing.ImageUrl;
            }

            if (obj.Id == 0) { _db.Banners.Add(obj); TempData["success"] = "Banner created."; }
            else { _db.Banners.Update(obj); TempData["success"] = "Banner updated."; }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int? id) => id == null ? NotFound() : View(_db.Banners.Find(id));

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            var b = await _db.Banners.FindAsync(id);
            if (b == null) return NotFound();
            _db.Banners.Remove(b);
            await _db.SaveChangesAsync();
            TempData["success"] = "Banner deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
