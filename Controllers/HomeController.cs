using ContractorHub.Data;
using ContractorHub.Models;
using ContractorHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public HomeController(
            AppDbContext context,
            IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        [Authorize(Policy = PermissionPolicies.DashboardView)]
        public async Task<IActionResult> Index()
        {
            var canViewClients = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.ClientsView)).Succeeded;
            var canViewProducts = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.ProductsView)).Succeeded;
            var canViewOffers = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.OffersView)).Succeeded;
            var canViewContracts = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.ContractsView)).Succeeded;
            var canViewDeals = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.DealsView)).Succeeded;

            ViewBag.ClientCount = canViewClients
                ? await _context.Clients.CountAsync()
                : 0;

            ViewBag.ProductCount = canViewProducts
                ? await _context.Products.CountAsync()
                : 0;

            ViewBag.OfferCount = canViewOffers
                ? await _context.CommercialOffers.CountAsync()
                : 0;

            ViewBag.ContractCount = canViewContracts
                ? await _context.Contracts.CountAsync()
                : 0;

            ViewBag.DealCount = canViewDeals
                ? await _context.Deals.CountAsync()
                : 0;

            if (canViewDeals)
            {
                var now = DateTime.UtcNow;
                var overdueStatuses = new[] { "Успешна", "Закрыта", "Отменена" };

                ViewBag.OverdueDealCount = await _context.Deals
                    .CountAsync(d => d.NextActionAt.HasValue
                        && d.NextActionAt.Value < now
                        && !overdueStatuses.Contains(d.Status));

                ViewBag.OverdueDeals = await _context.Deals
                    .Include(d => d.Client)
                    .Where(d => d.NextActionAt.HasValue
                        && d.NextActionAt.Value < now
                        && !overdueStatuses.Contains(d.Status))
                    .OrderBy(d => d.NextActionAt)
                    .Take(6)
                    .ToListAsync();
            }
            else
            {
                ViewBag.OverdueDealCount = 0;
                ViewBag.OverdueDeals = new List<Deal>();
            }

            return View();
        }

        [Authorize(Policy = PermissionPolicies.DashboardView)]
        public IActionResult Privacy() => View();

        [Authorize(Policy = PermissionPolicies.DashboardView)]
        public IActionResult About() => View();
    }
}
