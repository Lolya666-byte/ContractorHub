using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;

namespace ContractorHub.Controllers
{
	public class DashboardController : Controller
	{
		private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{

			var totalOffers = await _context.CommercialOffers.CountAsync();

			var approvedOffers = await _context.CommercialOffers
				.CountAsync(o => o.Status == "Согласовано");

			var totalContracts = await _context.Contracts.CountAsync();

			var activeContracts = await _context.Contracts
				.CountAsync(c => c.Status == "Активен");

			var totalContractAmount = (decimal)await _context.Contracts
				.Select(c => (double)c.TotalAmount)
				.SumAsync();

			var totalOfferAmount = (decimal)await _context.CommercialOffers
				.Select(o => (double)o.TotalAmount)
				.SumAsync();

			ViewBag.TotalOffers = totalOffers;
			ViewBag.ApprovedOffers = approvedOffers;
			ViewBag.TotalContracts = totalContracts;
			ViewBag.ActiveContracts = activeContracts;
			ViewBag.TotalContractAmount = totalContractAmount;
			ViewBag.TotalOfferAmount = totalOfferAmount;

			var offersByMonth = await _context.CommercialOffers
				.GroupBy(o => new
				{
					o.Date.Year,
					o.Date.Month
				})
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


			var topClients = await _context.Contracts
				.Include(c => c.Client)
				.GroupBy(c => c.ClientId)
				.Select(g => new
				{
					ClientName = g
						.Select(x => x.Client != null
							? x.Client.Name
							: "Неизвестно")
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
				.Select(status =>
					contractStatuses
						.FirstOrDefault(x => x.Status == status)?.Count ?? 0)
				.ToList();


			return View();
		}
	}

}
