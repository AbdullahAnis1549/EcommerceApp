using EcommerceApp.Data;
using EcommerceApp.Helpers;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            user.Email = user.Email.Trim().ToLowerInvariant();
            user.Phone = user.Phone.Trim();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == user.Email);

            if (existingUser != null)
            {
                if (existingUser.VerifyStatus == true)
                {
                    ModelState.AddModelError("Email", "This email is already registered. Please login instead.");
                    return View(user);
                }

                return await ResendVerificationAndRedirect(existingUser);
            }

            if (await _context.Users.AnyAsync(u => u.Phone == user.Phone))
            {
                ModelState.AddModelError("Phone", "This phone number is already registered.");
                return View(user);
            }

            int verifyCode = GenerateVerificationCode();

            user.UserRole = SD.RoleUser;
            user.VerifyStatus = false;
            user.VerifyCode = verifyCode;
            user.VerifyCodeExpDate = DateTime.Now.AddMinutes(15);
            user.Password = PasswordHelper.Hash(user.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            try
            {
                _emailService.SendVerificationEmail(user.Email, verifyCode);
                TempData["SuccessMessage"] = "Verification code sent to your email. Please verify to continue.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Account created but email could not be sent. Check EmailSettings in appsettings.json.";
            }

            return RedirectToAction(nameof(VerifyUser), new { email = user.Email });
        }

        [HttpGet]
        public IActionResult VerifyUser(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(Register));

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUser(string email, string verifyCode)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(verifyCode))
            {
                TempData["ErrorMessage"] = "Email and verification code are required.";
                return RedirectToAction(nameof(VerifyUser), new { email });
            }

            if (!int.TryParse(verifyCode.Trim(), out int code))
            {
                TempData["ErrorMessage"] = "Invalid verification code format.";
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(Register));
            }

            if (user.VerifyStatus == true)
            {
                TempData["SuccessMessage"] = "Your email is already verified. Please login.";
                return RedirectToAction(nameof(Login));
            }

            if (user.VerifyCode != code)
            {
                TempData["ErrorMessage"] = "Invalid verification code.";
                ViewBag.Email = email;
                return View();
            }

            if (user.VerifyCodeExpDate == null || user.VerifyCodeExpDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Verification code has expired. Please register again.";
                return RedirectToAction(nameof(Register));
            }

            user.VerifyStatus = true;
            user.VerifyCode = null;
            user.VerifyCodeExpDate = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Email and Password are required.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null || !PasswordHelper.Verify(password, user.Password))
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return View();
            }

            if (user.IsBlocked)
            {
                TempData["ErrorMessage"] = "Your account has been blocked. Please contact support.";
                return View();
            }

            if (!user.Password.StartsWith("$2"))
            {
                user.Password = PasswordHelper.Hash(password);
                await _context.SaveChangesAsync();
            }

            if (user.VerifyStatus != true)
            {
                TempData["ErrorMessage"] = "Please verify your email before logging in.";
                return RedirectToAction(nameof(VerifyUser), new { email = user.Email });
            }

            var role = (user.UserRole ?? SD.RoleUser).Trim().ToLowerInvariant();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            if (role == SD.RoleAdmin)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Home", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity!;
            var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null) return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            if (user.UserProfile == null)
                user.UserProfile = new UserProfile { UserId = user.Id };

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User user, string fullAddress, string city, string state, string postalCode, DateTime? dateOfBirth, string? gender, string? bio)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user.Id != currentUserId)
                return Unauthorized();

            var existingUser = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (existingUser == null)
                return NotFound();

            existingUser.Name = user.Name;
            existingUser.Phone = user.Phone;

            if (existingUser.UserProfile == null)
            {
                existingUser.UserProfile = new UserProfile
                {
                    UserId = existingUser.Id,
                    FullAddress = fullAddress ?? string.Empty,
                    City = city ?? string.Empty,
                    State = state ?? string.Empty,
                    PostalCode = postalCode ?? string.Empty,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    Bio = bio
                };
                _context.UserProfiles.Add(existingUser.UserProfile);
            }
            else
            {
                existingUser.UserProfile.FullAddress = fullAddress ?? string.Empty;
                existingUser.UserProfile.City = city ?? string.Empty;
                existingUser.UserProfile.State = state ?? string.Empty;
                existingUser.UserProfile.PostalCode = postalCode ?? string.Empty;
                existingUser.UserProfile.DateOfBirth = dateOfBirth;
                existingUser.UserProfile.Gender = gender;
                existingUser.UserProfile.Bio = bio;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email address.";
                return View();
            }

            string resetCode = GenerateResetCode();
            user.ForgotCode = resetCode;
            user.ForgotCodeExp = DateTime.Now.AddMinutes(30);

            await _context.SaveChangesAsync();

            try
            {
                _emailService.SendPasswordResetEmail(user.Email, resetCode);
                TempData["SuccessMessage"] = "Reset code sent to your email.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not send email. Please check EmailSettings in appsettings.json.";
                return View();
            }

            return RedirectToAction(nameof(ResetCode), new { email = user.Email });
        }

        [HttpGet]
        public IActionResult ResetCode(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(ForgotPassword));

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetCode(string email, string resetCode)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(resetCode))
            {
                TempData["ErrorMessage"] = "Email and reset code are required.";
                return RedirectToAction(nameof(ResetCode), new { email });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (string.IsNullOrEmpty(user.ForgotCode) || user.ForgotCode != resetCode.Trim())
            {
                TempData["ErrorMessage"] = "Invalid reset code.";
                ViewBag.Email = email;
                return View();
            }

            if (user.ForgotCodeExp == null || user.ForgotCodeExp < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Reset code has expired. Please request a new one.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return RedirectToAction(nameof(ResetPassword), new { email, resetCode = resetCode.Trim() });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email, string? resetCode)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(resetCode))
                return RedirectToAction(nameof(ForgotPassword));

            ViewBag.Email = email;
            ViewBag.ResetCode = resetCode;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string resetCode, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(resetCode))
            {
                TempData["ErrorMessage"] = "Invalid reset session.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["ErrorMessage"] = "Password fields are required.";
                ViewBag.Email = email;
                ViewBag.ResetCode = resetCode;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                ViewBag.Email = email;
                ViewBag.ResetCode = resetCode;
                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters.";
                ViewBag.Email = email;
                ViewBag.ResetCode = resetCode;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (user.ForgotCode != resetCode.Trim())
            {
                TempData["ErrorMessage"] = "Invalid reset code.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (user.ForgotCodeExp == null || user.ForgotCodeExp < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Reset code has expired. Please request a new one.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            user.Password = PasswordHelper.Hash(newPassword);
            user.ForgotCode = null;
            user.ForgotCodeExp = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password reset successfully! Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        private async Task<IActionResult> ResendVerificationAndRedirect(User user)
        {
            int verifyCode = GenerateVerificationCode();
            user.VerifyCode = verifyCode;
            user.VerifyCodeExpDate = DateTime.Now.AddMinutes(15);
            await _context.SaveChangesAsync();

            try
            {
                _emailService.SendVerificationEmail(user.Email, verifyCode);
                TempData["SuccessMessage"] = "This email is already registered but not verified. A new verification code has been sent.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not send verification email. Check EmailSettings in appsettings.json.";
            }

            return RedirectToAction(nameof(VerifyUser), new { email = user.Email });
        }

        private static int GenerateVerificationCode() => Random.Shared.Next(100000, 999999);

        private static string GenerateResetCode() => Random.Shared.Next(100000, 999999).ToString();
    }
}
