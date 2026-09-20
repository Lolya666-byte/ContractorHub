using System.Security.Claims;
using ContractorHub.Data;
using ContractorHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class PermissionsController : Controller
    {
        private static readonly string[] Roles =
        {
            "Administrator",
            "Manager",
            "Accountant"
        };

        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var grants = await _context.RolePermissions
                .AsNoTracking()
                .ToListAsync();

            var grantedPairs = grants
                .Select(x => $"{x.Role}:{x.PermissionId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var model = new PermissionMatrixViewModel
            {
                Roles = Roles.ToList(),
                Permissions = permissions.Select(permission => new PermissionMatrixRowViewModel
                {
                    Id = permission.Id,
                    Code = permission.Code,
                    Name = permission.Name,
                    Category = permission.Category,
                    Granted = Roles.ToDictionary(
                        role => role,
                        role => grantedPairs.Contains($"{role}:{permission.Id}")
                    )
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            Dictionary<string, List<int>> selectedPermissions)
        {
            selectedPermissions ??= new Dictionary<string, List<int>>();

            var allowedRoleSet = Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var submitted = selectedPermissions
                .Where(x => allowedRoleSet.Contains(x.Key))
                .ToDictionary(
                    x => Roles.First(role => string.Equals(role, x.Key, StringComparison.OrdinalIgnoreCase)),
                    x => (x.Value ?? new List<int>()).Distinct().ToHashSet()
                );

            foreach (var role in Roles)
            {
                if (!submitted.ContainsKey(role))
                    submitted[role] = new HashSet<int>();
            }

            var permissionIds = (await _context.Permissions
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync())
                .ToHashSet();

            var desired = new HashSet<(string Role, int PermissionId)>();
            foreach (var role in Roles)
            {
                foreach (var permissionId in submitted[role].Where(permissionIds.Contains))
                    desired.Add((Role: role, PermissionId: permissionId));
            }

            var existing = await _context.RolePermissions.ToListAsync();

            var toRemove = existing
                .Where(x => !desired.Contains((Role: x.Role, PermissionId: x.PermissionId)))
                .ToList();

            var existingKeys = existing
                .Select(x => (Role: x.Role, PermissionId: x.PermissionId))
                .ToHashSet();

            var toAdd = desired
                .Where(key => !existingKeys.Contains(key))
                .Select(key => new RolePermission
                {
                    Role = key.Role,
                    PermissionId = key.PermissionId
                })
                .ToList();

            if (toRemove.Count > 0)
                _context.RolePermissions.RemoveRange(toRemove);

            if (toAdd.Count > 0)
                await _context.RolePermissions.AddRangeAsync(toAdd);

            await _context.SaveChangesAsync();

            var userId = int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var parsedUserId)
                ? parsedUserId
                : (int?)null;

            var userName = User.Identity?.Name ?? "Администратор";
            var changes = $"Обновлены права ролей: добавлено {toAdd.Count}, удалено {toRemove.Count}.";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = "Изменение прав доступа",
                EntityType = "RolePermission",
                Description = changes,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = changes;
            return RedirectToAction(nameof(Index));
        }
    }
}
