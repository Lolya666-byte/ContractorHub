using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;
using ContractorHub.Models;

namespace ContractorHub.Controllers
{
	public class CommercialOffersController : Controller
	{
		private readonly AppDbContext _context;

		public CommercialOffersController(AppDbContext context)
		{
			_context = context;
		}
		public async Task<IActionResult> Index(string searchString)
		{
			var offers = from o in _context.CommercialOffers
						 .Include(o => o.Client)
						 .Include(o => o.Items)
						 .ThenInclude(i => i.Product)
						 select o;

			if (!string.IsNullOrEmpty(searchString))
			{
				offers = offers.Where(o => o.OfferNumber.Contains(searchString) ||
										   o.Client.Name.Contains(searchString));
			}

			var offersList = await offers.ToListAsync();
			var offerViewModels = offersList.Select(o => new
			{
				Offer = o,
				HasContract = _context.Contracts.Any(c => c.CommercialOfferId == o.Id)
			}).ToList();
			ViewBag.HasContractMap = offerViewModels.ToDictionary(
				x => x.Offer.Id,
				x => x.HasContract
			);

			return View(offersList);
		}
		public IActionResult Create()
		{
			ViewBag.Clients = _context.Clients.ToList();
			ViewBag.Products = _context.Products.ToList();
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Create(CommercialOfferViewModel model)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Clients = _context.Clients.ToList();
				ViewBag.Products = _context.Products.ToList();
				return View(model);
			}

			foreach (var item in model.Items)
			{
				var product = await _context.Products.FindAsync(item.ProductId);
				if (product == null)
				{
					TempData["Error"] = $"Товар с ID {item.ProductId} не найден.";
					ViewBag.Clients = _context.Clients.ToList();
					ViewBag.Products = _context.Products.ToList();
					return View(model);
				}

				if (product.Stock < item.Quantity)
				{
					TempData["Error"] = $"Недостаточно товара '{product.Name}' на складе (доступно: {product.Stock}, запрошено: {item.Quantity})";
					ViewBag.Clients = _context.Clients.ToList();
					ViewBag.Products = _context.Products.ToList();
					return View(model);
				}
			}

			var offer = new CommercialOffer
			{
				OfferNumber = GenerateOfferNumber(),
				ClientId = model.ClientId,
				Date = DateTime.Now,
				Status = "Черновик",
				TotalAmount = 0
			};

			_context.CommercialOffers.Add(offer);
			await _context.SaveChangesAsync();

			decimal total = 0;
			foreach (var item in model.Items)
			{
				var product = await _context.Products.FindAsync(item.ProductId);
				if (product != null)
				{
					product.Stock -= item.Quantity;
					_context.Update(product);

					var offerItem = new OfferItem
					{
						CommercialOfferId = offer.Id,
						ProductId = item.ProductId,
						Quantity = item.Quantity,
						Price = product.Price
					};
					_context.OfferItems.Add(offerItem);
					total += product.Price * item.Quantity;
				}
			}

			offer.TotalAmount = total;
			await _context.SaveChangesAsync();

			TempData["Success"] = $"КП №{offer.OfferNumber} создано на сумму {total:N2} руб. Остатки товаров обновлены!";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[HttpPost]
		public async Task<IActionResult> ChangeStatus(int id, string status)
		{
			var offer = await _context.CommercialOffers.FindAsync(id);
			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			if (status == "Согласовано")
			{
				TempData["Error"] = "Нельзя вручную установить статус 'Согласовано'. Статус меняется автоматически после создания договора.";
				return RedirectToAction("Index");
			}

			if (offer.Status == "Согласовано" && status != "Согласовано")
			{
				TempData["Error"] = "Нельзя изменить статус согласованного КП.";
				return RedirectToAction("Index");
			}

			offer.Status = status;
			await _context.SaveChangesAsync();
			TempData["Success"] = $"Статус КП №{offer.OfferNumber} изменён на '{status}'";
			return RedirectToAction("Index");
		}

		public async Task<IActionResult> Edit(int id)
		{
			var offer = await _context.CommercialOffers
				.Include(o => o.Items)
				.ThenInclude(i => i.Product)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			var model = new CommercialOfferViewModel
			{
				OfferNumber = offer.OfferNumber,
				ClientId = offer.ClientId,
				Items = offer.Items.Select(i => new OfferItemViewModel
				{
					ProductId = i.ProductId,
					Quantity = i.Quantity
				}).ToList()
			};

			ViewBag.Clients = await _context.Clients.ToListAsync();
			ViewBag.Products = await _context.Products.ToListAsync();
			ViewBag.OfferId = offer.Id;
			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(int id, CommercialOfferViewModel model)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Clients = await _context.Clients.ToListAsync();
				ViewBag.Products = await _context.Products.ToListAsync();
				ViewBag.OfferId = id;
				return View(model);
			}

			var offer = await _context.CommercialOffers
				.Include(o => o.Items)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			if (offer.Status == "Согласовано")
			{
				TempData["Error"] = "Нельзя редактировать согласованное КП.";
				return RedirectToAction("Index");
			}

			foreach (var oldItem in offer.Items)
			{
				var product = await _context.Products.FindAsync(oldItem.ProductId);
				if (product != null)
				{
					product.Stock += oldItem.Quantity;
					_context.Update(product);
				}
			}

			_context.OfferItems.RemoveRange(offer.Items);

			offer.OfferNumber = model.OfferNumber;
			offer.ClientId = model.ClientId;
			offer.TotalAmount = 0;

			decimal total = 0;
			foreach (var item in model.Items)
			{
				var product = await _context.Products.FindAsync(item.ProductId);
				if (product == null) continue;

				if (product.Stock < item.Quantity)
				{
					TempData["Error"] = $"Недостаточно товара '{product.Name}' (доступно: {product.Stock})";
					ViewBag.Clients = await _context.Clients.ToListAsync();
					ViewBag.Products = await _context.Products.ToListAsync();
					ViewBag.OfferId = id;
					return View(model);
				}

				product.Stock -= item.Quantity;
				_context.Update(product);

				var offerItem = new OfferItem
				{
					CommercialOfferId = offer.Id,
					ProductId = item.ProductId,
					Quantity = item.Quantity,
					Price = product.Price
				};
				_context.OfferItems.Add(offerItem);
				total += product.Price * item.Quantity;
			}

			offer.TotalAmount = total;
			await _context.SaveChangesAsync();

			TempData["Success"] = $"КП №{offer.OfferNumber} обновлено!";
			return RedirectToAction("Index");
		}
		[HttpPost]
		public async Task<IActionResult> ResetStatus(int id)
		{
			var offer = await _context.CommercialOffers.FindAsync(id);
			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			if (offer.Status != "Согласовано")
			{
				TempData["Error"] = "Сброс доступен только для статуса 'Согласовано'.";
				return RedirectToAction("Index");
			}

			var contract = await _context.Contracts
				.FirstOrDefaultAsync(c => c.CommercialOfferId == offer.Id);

			if (contract != null)
			{
				TempData["Error"] = "Нельзя сбросить статус: на это КП уже создан договор.";
				return RedirectToAction("Index");
			}

			offer.Status = "Отправлено";
			await _context.SaveChangesAsync();

			TempData["Success"] = $"Статус КП №{offer.OfferNumber} сброшен на 'Отправлено'.";
			return RedirectToAction("Index");
		}
		private string GenerateOfferNumber()
		{
			string prefix = DateTime.Now.ToString("yyyy-MM");

			var lastOffer = _context.CommercialOffers
				.Where(o => o.OfferNumber.StartsWith(prefix))
				.OrderByDescending(o => o.OfferNumber)
				.FirstOrDefault();

			if (lastOffer == null)
			{
				return $"{prefix}-001";
			}

			string lastNumberStr = lastOffer.OfferNumber.Substring(lastOffer.OfferNumber.LastIndexOf('-') + 1);
			if (int.TryParse(lastNumberStr, out int lastNumber))
			{
				int newNumber = lastNumber + 1;
				return $"{prefix}-{newNumber:D3}";
			}

			return $"{prefix}-001";
		}
	}
}