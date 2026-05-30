using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = SD.RoleAdmin)]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CloudinaryService _cloudinary;

        public AboutController(ApplicationDbContext db, CloudinaryService cloudinary)
        {
            _db = db;
            _cloudinary = cloudinary;
        }

        public async Task<IActionResult> Edit() =>
            View("~/Views/Admin/About.cshtml", await GetOrCreatePageAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AboutPage model, IFormFile? file)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/About.cshtml", model);

            var page = await GetOrCreatePageAsync();

            page.Title = model.Title.Trim();
            page.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim();
            page.Body = model.Body.Trim();
            page.UpdatedAt = DateTime.UtcNow;

            if (file != null)
                page.ImageUrl = await _cloudinary.UploadImageAsync(file, "ecommerce-about");

            if (page.Id == 0)
                _db.AboutPages.Add(page);
            else
                _db.AboutPages.Update(page);

            await _db.SaveChangesAsync();
            TempData["success"] = "About page updated successfully.";
            return RedirectToAction(nameof(Edit));
        }

        private async Task<AboutPage> GetOrCreatePageAsync()
        {
            var page = await _db.AboutPages.OrderBy(p => p.Id).FirstOrDefaultAsync();
            if (page != null)
                return page;

            return new AboutPage
            {
                Title = "About Shopper.",
                Subtitle = "Your trusted online store",
                Body = "We are a modern online store offering quality products across electronics, fashion, home, sports, books, and beauty.\n\nOur mission is to deliver a smooth shopping experience with secure payments, fast checkout, and reliable customer support."
            };
        }
    }
}
