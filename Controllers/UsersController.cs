using System.Security.Claims;
using ContractorHub.Data;
using ContractorHub.Models;
using ContractorHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Controllers
{
	[Authorize(Policy = PermissionPolicies.UsersManage)]
	public class UsersController : Controller
	{
		private readonly AppDbContext _context;
		private readonly AuthService _authService;
		private readonly AuditService _auditService;

		public UsersController(
			AppDbContext context,
			AuthService authService,
			AuditService auditService)
		{
			_context = context;
			_authService = authService;
			_auditService = auditService;
		}

		public async Task<IActionResult> Index()
		{
			var users = await _context.Users
				.OrderBy(x => x.FullName)
				.ToListAsync();

			return View(users);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			string fullName,
			string username,
			string password,
			string role)
		{
			if (string.IsNullOrWhiteSpace(fullName) ||
				string.IsNullOrWhiteSpace(username) ||
				string.IsNullOrWhiteSpace(password) ||
				string.IsNullOrWhiteSpace(role))
			{
				ViewBag.Error = "Заполните все обязательные поля.";
				return View();
			}

			username = username.Trim();
			fullName = fullName.Trim();

			var existingUser = await _context.Users
				.FirstOrDefaultAsync(x =>
					x.Username.ToLower() == username.ToLower());

			if (existingUser != null)
			{
				ViewBag.Error =
					"Пользователь с таким логином уже существует.";

				return View();
			}

			var allowedRoles = new[]
			{
				"Administrator",
				"Manager",
				"Accountant"
			};

			if (!allowedRoles.Contains(role))
			{
				ViewBag.Error = "Указана недопустимая роль.";
				return View();
			}

			var user = new User
			{
				FullName = fullName,
				Username = username,
				Role = role,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			user.PasswordHash =
				_authService.HashPassword(user, password);

			_context.Users.Add(user);

			await _context.SaveChangesAsync();

			await LogAction(
				"CreateUser",
				$"Создан сотрудник «{user.FullName}» " +
				$"с ролью «{user.Role}».",
				user.Id);

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
				return NotFound();

			return View(user);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			int id,
			string fullName,
			string username,
			string role,
			bool isActive)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
				return NotFound();

			if (string.IsNullOrWhiteSpace(fullName) ||
				string.IsNullOrWhiteSpace(username) ||
				string.IsNullOrWhiteSpace(role))
			{
				ViewBag.Error = "Заполните все обязательные поля.";
				return View(user);
			}

			var allowedRoles = new[]
			{
				"Administrator",
				"Manager",
				"Accountant"
			};

			if (!allowedRoles.Contains(role))
			{
				ViewBag.Error = "Указана недопустимая роль.";
				return View(user);
			}

			username = username.Trim();
			fullName = fullName.Trim();

			var existingUser = await _context.Users
				.FirstOrDefaultAsync(x =>
					x.Id != id &&
					x.Username.ToLower() == username.ToLower());

			if (existingUser != null)
			{
				ViewBag.Error =
					"Пользователь с таким логином уже существует.";

				return View(user);
			}

			var currentUserId =
				User.FindFirstValue(
					ClaimTypes.NameIdentifier);

			if (currentUserId == user.Id.ToString() &&
				!isActive)
			{
				ViewBag.Error =
					"Нельзя отключить собственную учётную запись.";

				return View(user);
			}

			var oldFullName = user.FullName;
			var oldUsername = user.Username;
			var oldRole = user.Role;
			var oldIsActive = user.IsActive;

			user.FullName = fullName;
			user.Username = username;
			user.Role = role;
			user.IsActive = isActive;

			await _context.SaveChangesAsync();

			var changes = new List<string>();

			if (oldFullName != user.FullName)
			{
				changes.Add(
					$"ФИО: «{oldFullName}» → «{user.FullName}»");
			}

			if (oldUsername != user.Username)
			{
				changes.Add(
					$"Логин: «{oldUsername}» → «{user.Username}»");
			}

			if (oldRole != user.Role)
			{
				changes.Add(
					$"Роль: «{oldRole}» → «{user.Role}»");
			}

			if (oldIsActive != user.IsActive)
			{
				changes.Add(
					user.IsActive
						? "Учётная запись активирована"
						: "Учётная запись заблокирована");
			}

			if (changes.Count == 0)
			{
				changes.Add("Изменений нет");
			}

			await LogAction(
				"EditUser",
				$"Изменён сотрудник «{user.FullName}». " +
				string.Join("; ", changes),
				user.Id);

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleActive(int id)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
				return NotFound();

			var currentUserId =
				User.FindFirstValue(
					ClaimTypes.NameIdentifier);

			if (currentUserId == user.Id.ToString())
			{
				TempData["Error"] =
					"Нельзя отключить собственную учётную запись.";

				return RedirectToAction(nameof(Index));
			}

			user.IsActive = !user.IsActive;

			await _context.SaveChangesAsync();

			await LogAction(
				"ToggleUser",
				$"Статус сотрудника «{user.FullName}»: " +
				(user.IsActive
					? "активирован."
					: "заблокирован."),
				user.Id);

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> ResetPassword(int id)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
				return NotFound();

			return View(user);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(
			int id,
			string newPassword,
			string confirmPassword)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
				return NotFound();

			if (string.IsNullOrWhiteSpace(newPassword))
			{
				ViewBag.Error = "Введите новый пароль.";
				return View(user);
			}

			if (newPassword != confirmPassword)
			{
				ViewBag.Error = "Пароли не совпадают.";
				return View(user);
			}

			if (newPassword.Length < 8)
			{
				ViewBag.Error =
					"Пароль должен содержать минимум 8 символов.";

				return View(user);
			}

			if (!newPassword.Any(char.IsUpper))
			{
				ViewBag.Error =
					"Пароль должен содержать хотя бы одну заглавную букву.";

				return View(user);
			}

			if (!newPassword.Any(char.IsLower))
			{
				ViewBag.Error =
					"Пароль должен содержать хотя бы одну строчную букву.";

				return View(user);
			}

			if (!newPassword.Any(char.IsDigit))
			{
				ViewBag.Error =
					"Пароль должен содержать хотя бы одну цифру.";

				return View(user);
			}

			user.PasswordHash =
				_authService.HashPassword(user, newPassword);

			await _context.SaveChangesAsync();

			await LogAction(
				"ResetPassword",
				$"Сброшен пароль сотрудника «{user.FullName}».",
				user.Id);

			return RedirectToAction(nameof(Index));
		}

		private async Task LogAction(
			string action,
			string description,
			int? entityId = null)
		{
			var currentUserId =
				User.FindFirstValue(
					ClaimTypes.NameIdentifier);

			int? userId = null;

			if (int.TryParse(currentUserId, out var parsedId))
			{
				userId = parsedId;
			}

			await _auditService.LogAsync(
				userId,
				User.Identity?.Name,
				action,
				description,
				"User",
				entityId);
		}
	}
}