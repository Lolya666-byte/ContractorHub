using ContractorHub.Services;

using ContractorHub.Data;
using ContractorHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Controllers
{
	[Authorize(Policy = PermissionPolicies.AuditView)]
	public class AuditController : Controller
	{
		private readonly AppDbContext _context;

		public AuditController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(
			string? search,
			[FromQuery(Name = "action")] string? actionFilter)
		{
			search = search?.Trim();
			actionFilter = actionFilter?.Trim();

			var logs = await _context.AuditLogs
				.AsNoTracking()
				.OrderByDescending(x => x.CreatedAt)
				.Select(x => new AuditLogViewModel
				{
					Id = x.Id,
					UserName = x.UserName,
					Action = x.Action,
					EntityType = x.EntityType,
					Description = x.Description,
					CreatedAt = x.CreatedAt
				})
				.ToListAsync();

			if (!string.IsNullOrWhiteSpace(search))
			{
				logs = logs
					.Where(x =>
						(x.UserName ?? "").Contains(
							search,
							StringComparison.OrdinalIgnoreCase) ||
						(x.Action ?? "").Contains(
							search,
							StringComparison.OrdinalIgnoreCase) ||
						(x.Description ?? "").Contains(
							search,
							StringComparison.OrdinalIgnoreCase) ||
						(x.EntityType ?? "").Contains(
							search,
							StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			if (!string.IsNullOrWhiteSpace(actionFilter))
			{
				logs = logs
					.Where(x => string.Equals(
						x.Action,
						actionFilter,
						StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			ViewBag.Search = search ?? string.Empty;
			ViewBag.Action = actionFilter ?? string.Empty;

			return View(logs);
		}
	}
}