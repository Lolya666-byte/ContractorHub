using Microsoft.AspNetCore.Mvc;
using ContractorHub.Data;

namespace ContractorHub.Controllers
{
	public class HomeController : Controller
	{
		private readonly AppDbContext _context;

		public HomeController(AppDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			var clientCount = _context.Clients.Count();
			var productCount = _context.Products.Count();
			var offerCount = _context.CommercialOffers.Count();
			var contractCount = _context.Contracts.Count();

			ViewBag.ClientCount = clientCount;
			ViewBag.ProductCount = productCount;
			ViewBag.OfferCount = offerCount;
			ViewBag.ContractCount = contractCount;

			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}
		public IActionResult About()
		{
			return View();
		}
	}
}