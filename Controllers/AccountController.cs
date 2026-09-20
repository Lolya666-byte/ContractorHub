using System.Security.Claims;
using ContractorHub.Models;
using ContractorHub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;

namespace ContractorHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;

        public AccountController(
            AuthService authService,
            AppDbContext context,
            AuditService auditService)
        {
            _authService = authService;
            _context = context;
            _auditService = auditService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password,
            string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Введите логин и пароль.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var result = await _authService.AuthenticateAsync(
                username.Trim(),
                password);

            var user = result.User;

            if (user == null)
            {
                ViewBag.Error = "Неверный логин или пароль.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Username", user.Username)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "Login",
                "Выполнен вход в систему.",
                "User",
                user.Id);

            if (result.RequiresPasswordChange)
            {
                TempData["PasswordNotice"] =
                    "Пароль был перенесён на новый защищённый формат. Установите новый пароль.";

                return RedirectToAction(nameof(ChangePassword));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.Notice = TempData["PasswordNotice"]?.ToString();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Заполните все поля.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Новый пароль и подтверждение не совпадают.";
                return View();
            }

            if (!_authService.IsPasswordStrongEnough(newPassword, out var passwordError))
            {
                ViewBag.Error = passwordError;
                return View();
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);

            if (user == null || !user.IsActive)
                return Unauthorized();

            if (_authService.IsLegacyHash(user.PasswordHash))
            {
                var legacyResult = await _authService.AuthenticateAsync(
                    user.Username,
                    currentPassword);

                if (legacyResult.User == null)
                {
                    ViewBag.Error = "Текущий пароль указан неверно.";
                    return View();
                }
            }
            else if (!_authService.VerifyPassword(user, currentPassword))
            {
                ViewBag.Error = "Текущий пароль указан неверно.";
                return View();
            }

            user.PasswordHash = _authService.HashPassword(user, newPassword);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user.Id,
                user.FullName,
                "ChangePassword",
                "Пользователь изменил собственный пароль.",
                "User",
                user.Id);

            TempData["Success"] = "Пароль успешно изменён.";

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdValue, out var userId))
            {
                await _auditService.LogAsync(
                    userId,
                    User.Identity?.Name,
                    "Logout",
                    "Выполнен выход из системы.",
                    "User",
                    userId);
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
