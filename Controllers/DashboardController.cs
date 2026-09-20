using ContractorHub.Data;
using ContractorHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public DashboardController(
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

            var canViewOffers = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.OffersView)).Succeeded;

            var canViewContracts = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.ContractsView)).Succeeded;

            var canViewDeals = (await _authorizationService
                .AuthorizeAsync(User, PermissionPolicies.DealsView)).Succeeded;

            ViewBag.CanViewClients = canViewClients;
            ViewBag.CanViewOffers = canViewOffers;
            ViewBag.CanViewContracts = canViewContracts;
            ViewBag.CanViewDeals = canViewDeals;

            ViewBag.TotalOffers = canViewOffers
                ? await _context.CommercialOffers.CountAsync()
                : 0;

            ViewBag.ApprovedOffers = canViewOffers
                ? await _context.CommercialOffers.CountAsync(o => o.Status == "Согласовано")
                : 0;

            ViewBag.TotalContracts = canViewContracts
                ? await _context.Contracts.CountAsync()
                : 0;

            ViewBag.ActiveContracts = canViewContracts
                ? await _context.Contracts.CountAsync(c => c.Status == "Активен")
                : 0;

            ViewBag.TotalContractAmount = canViewContracts
                ? (decimal)await _context.Contracts
                    .Select(c => (double)c.TotalAmount)
                    .SumAsync()
                : 0m;

            ViewBag.TotalDeals = canViewDeals
                ? await _context.Deals.CountAsync()
                : 0;

            ViewBag.ActiveDeals = canViewDeals
                ? await _context.Deals.CountAsync(d => d.Status != "Успешна" && d.Status != "Закрыта" && d.Status != "Отменена")
                : 0;

            ViewBag.ActiveDealAmount = canViewDeals
                ? (decimal)await _context.Deals
                    .Where(d => d.Status != "Успешна" && d.Status != "Закрыта" && d.Status != "Отменена")
                    .Select(d => (double)d.Amount)
                    .SumAsync()
                : 0m;

            ViewBag.SuccessfulDealAmount = canViewDeals
                ? (decimal)await _context.Deals
                    .Where(d => d.Status == "Успешна")
                    .Select(d => (double)d.Amount)
                    .SumAsync()
                : 0m;

            ViewBag.TotalOfferAmount = canViewOffers
                ? (decimal)await _context.CommercialOffers
                    .Select(o => (double)o.TotalAmount)
                    .SumAsync()
                : 0m;

            if (canViewOffers)
            {
                var offersByMonth = await _context.CommercialOffers
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(o => (double)o.TotalAmount)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                ViewBag.OfferLabels = offersByMonth
                    .Select(x => $"{x.Month:D2}.{x.Year}")
                    .ToList();

                ViewBag.OfferValues = offersByMonth
                    .Select(x => x.Total)
                    .ToList();
            }
            else
            {
                ViewBag.OfferLabels = new List<string>();
                ViewBag.OfferValues = new List<double>();
            }

            if (canViewClients && canViewContracts)
            {
                var topClients = await _context.Contracts
                    .Include(c => c.Client)
                    .GroupBy(c => c.ClientId)
                    .Select(g => new
                    {
                        ClientName = g
                            .Select(x => x.Client != null ? x.Client.Name : "Неизвестно")
                            .FirstOrDefault(),
                        Total = g.Sum(c => (double)c.TotalAmount)
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToListAsync();

                ViewBag.ClientNames = topClients
                    .Select(x => x.ClientName ?? "Неизвестно")
                    .ToList();

                ViewBag.ClientTotals = topClients
                    .Select(x => x.Total)
                    .ToList();
            }
            else
            {
                ViewBag.ClientNames = new List<string>();
                ViewBag.ClientTotals = new List<double>();
            }

            if (canViewDeals)
            {
                var dealStatuses = new[]
                {
                    "Новая",
                    "В работе",
                    "КП подготовлено",
                    "КП отправлено",
                    "На согласовании",
                    "Договор",
                    "Успешна",
                    "Закрыта",
                    "Отменена"
                };

                var dealStatusCounts = await _context.Deals
                    .GroupBy(d => d.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                ViewBag.DealStatusLabels = dealStatuses;
                ViewBag.DealStatusValues = dealStatuses
                    .Select(status => dealStatusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0)
                    .ToList();
            }
            else
            {
                ViewBag.DealStatusLabels = new[]
                {
                    "Новая", "В работе", "КП подготовлено", "КП отправлено",
                    "На согласовании", "Договор", "Успешна", "Закрыта", "Отменена"
                };
                ViewBag.DealStatusValues = new List<int>();
            }

            if (canViewContracts)
            {
                var contractStatuses = await _context.Contracts
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var statuses = new[]
                {
                    "Черновик",
                    "На подписи",
                    "Активен",
                    "Закрыт"
                };

                ViewBag.ContractStatusLabels = statuses;
                ViewBag.ContractStatusValues = statuses
                    .Select(status => contractStatuses
                        .FirstOrDefault(x => x.Status == status)?.Count ?? 0)
                    .ToList();
            }
            else
            {
                ViewBag.ContractStatusLabels = new[]
                {
                    "Черновик",
                    "На подписи",
                    "Активен",
                    "Закрыт"
                };
                ViewBag.ContractStatusValues = new List<int>();
            }

            return View();
        }
    }
}
