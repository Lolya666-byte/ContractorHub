using ContractorHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;
using ContractorHub.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorHub.Controllers
{
	public class CommercialOffersController : Controller
	{
		private readonly AppDbContext _context;
		private readonly AuditService _auditService;

		public CommercialOffersController(AppDbContext context, AuditService auditService)
		{
			_context = context;
			_auditService = auditService;
		}
		[Authorize(Policy = PermissionPolicies.OffersView)]
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
		[HttpGet]
		[Authorize(Policy = PermissionPolicies.OffersView)]
		public async Task<IActionResult> DownloadOffer(int id)
		{
			var offer = await _context.CommercialOffers
				.Include(o => o.Client)
				.Include(o => o.Items)
					.ThenInclude(i => i.Product)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			var document = Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(2, Unit.Centimetre);
					page.DefaultTextStyle(TextStyle.Default.FontSize(11));

					page.Header()
						.Column(header =>
						{
							header.Item().Text("КОММЕРЧЕСКОЕ ПРЕДЛОЖЕНИЕ")
								.FontSize(18)
								.Bold()
								.AlignCenter()
								.FontColor(Colors.Blue.Darken2);

							header.Item().PaddingTop(4).Text($"№ {offer.OfferNumber} от {offer.Date:dd.MM.yyyy}")
								.FontSize(12)
								.AlignCenter();
						});

					page.Content()
						.PaddingVertical(1, Unit.Centimetre)
						.Column(col =>
						{
							col.Item().Text("ИНФОРМАЦИЯ О КЛИЕНТЕ").Bold().FontSize(14);
							col.Item().PaddingTop(4).Text($"Клиент: {offer.Client?.Name ?? "Не указан"}");
							col.Item().Text($"ИНН: {offer.Client?.Inn ?? "—"}");
							col.Item().Text($"Контактное лицо: {offer.Client?.ContactPerson ?? "—"}");
							col.Item().Text($"Телефон: {offer.Client?.Phone ?? "—"}");
							col.Item().Text($"Email: {offer.Client?.Email ?? "—"}");
							col.Item().Text($"Адрес: {offer.Client?.Address ?? "—"}");

							col.Item().PaddingTop(1, Unit.Centimetre).Text("ПЕРЕЧЕНЬ ТОВАРОВ").Bold().FontSize(14);

							col.Item().PaddingTop(4).Table(table =>
							{
								table.ColumnsDefinition(columns =>
								{
									columns.ConstantColumn(35);
									columns.RelativeColumn(4);
									columns.RelativeColumn(1.2f);
									columns.RelativeColumn(1);
									columns.RelativeColumn(1.4f);
								});

								table.Header(header =>
								{
									header.Cell().Text("№").Bold().AlignCenter();
									header.Cell().Text("Наименование").Bold();
									header.Cell().Text("Цена, ₽").Bold().AlignRight();
									header.Cell().Text("Кол-во").Bold().AlignCenter();
									header.Cell().Text("Сумма, ₽").Bold().AlignRight();
								});

								var number = 1;
								foreach (var item in offer.Items ?? new List<OfferItem>())
								{
									table.Cell().Text(number++.ToString()).AlignCenter();
									table.Cell().Text(item.Product?.Name ?? "—");
									table.Cell().Text(item.Price.ToString("N2")).AlignRight();
									table.Cell().Text(item.Quantity.ToString()).AlignCenter();
									table.Cell().Text((item.Price * item.Quantity).ToString("N2")).AlignRight();
								}

								table.Cell().ColumnSpan(4).Text("ИТОГО:").Bold().AlignRight();
								table.Cell().Text(offer.TotalAmount.ToString("N2") + " ₽").Bold().AlignRight();
							});

							col.Item().PaddingTop(1, Unit.Centimetre).Text($"Статус КП: {offer.Status}");
							col.Item().PaddingTop(2, Unit.Centimetre).Text("Предложение действительно в соответствии с условиями, согласованными с клиентом.");

							col.Item().PaddingTop(2, Unit.Centimetre).Row(row =>
							{
								row.RelativeItem().Text("______________________\nОтветственный менеджер");
								row.RelativeItem().Text("______________________\nКлиент").AlignRight();
							});

							col.Item().PaddingTop(1, Unit.Centimetre)
								.Text($"Сформировано в Contractor Hub {DateTime.Now:dd.MM.yyyy HH:mm}")
								.FontSize(9)
								.FontColor(Colors.Grey.Medium);
						});

					page.Footer()
						.AlignCenter()
						.Text("ООО «Contractor Hub» • Все права защищены")
						.FontSize(9)
						.FontColor(Colors.Grey.Medium);
				});
			});

			var pdfBytes = document.GeneratePdf();
			await LogAuditAsync("DownloadOffer", $"Скачан PDF коммерческого предложения №{offer.OfferNumber}.", "CommercialOffer", offer.Id);
			return File(pdfBytes, "application/pdf", $"КП_{offer.OfferNumber}_{DateTime.Now:yyyyMMdd}.pdf");
		}

		[Authorize(Policy = PermissionPolicies.OffersManage)]
		public IActionResult Create()
		{
			ViewBag.Clients = _context.Clients.ToList();
			ViewBag.Products = _context.Products.ToList();
			return View();
		}

		[HttpPost]
		[Authorize(Policy = PermissionPolicies.OffersManage)]
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
				Date = DateTime.UtcNow,
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
			await LogAuditAsync("CreateOffer", $"Создано коммерческое предложение №{offer.OfferNumber} на сумму {total:N2} руб.", "CommercialOffer", offer.Id);

			TempData["Success"] = $"КП №{offer.OfferNumber} создано на сумму {total:N2} руб. Остатки товаров обновлены!";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Policy = PermissionPolicies.OffersManage)]
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
			var deal = await _context.Deals.FirstOrDefaultAsync(d => d.CommercialOfferId == offer.Id);
			if (deal != null)
			{
				var mappedStatus = status switch
				{
					"Отправлено" => "КП отправлено",
					"Согласовано" => "Договор",
					_ => (string?)null
				};
				if (mappedStatus != null && deal.Status != "Успешна" && deal.Status != "Закрыта" && deal.Status != "Отменена")
				{
					var oldStatus = deal.Status;
					deal.Status = mappedStatus;
					deal.NextAction = mappedStatus == "КП отправлено" ? "Связаться с клиентом по КП" : "Подготовить договор";
					deal.NextActionAt = DateTime.UtcNow.AddDays(mappedStatus == "КП отправлено" ? 2 : 1);
					_context.DealHistories.Add(new DealHistory { DealId = deal.Id, FromStatus = oldStatus, ToStatus = mappedStatus, Comment = $"Статус сделки обновлён по КП №{offer.OfferNumber}.", ChangedByUserId = GetCurrentUserId(), CreatedAt = DateTime.UtcNow });
				}
			}
			await _context.SaveChangesAsync();
			await LogAuditAsync("ChangeOfferStatus", $"Для КП №{offer.OfferNumber} установлен статус «{status}».", "CommercialOffer", offer.Id);
			TempData["Success"] = $"Статус КП №{offer.OfferNumber} изменён на '{status}'";
			return RedirectToAction("Index");
		}

		[Authorize(Policy = PermissionPolicies.OffersManage)]
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
		[Authorize(Policy = PermissionPolicies.OffersManage)]
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
			await LogAuditAsync("UpdateOffer", $"Обновлено коммерческое предложение №{offer.OfferNumber}. Сумма: {total:N2} руб.", "CommercialOffer", offer.Id);

			var linkedDeal = await _context.Deals.FirstOrDefaultAsync(d => d.CommercialOfferId == offer.Id);
			if (linkedDeal != null)
			{
				linkedDeal.Amount = offer.TotalAmount;
				await _context.SaveChangesAsync();
			}

			TempData["Success"] = $"КП №{offer.OfferNumber} обновлено!";
			return RedirectToAction("Index");
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = PermissionPolicies.OffersManage)]
		public async Task<IActionResult> Delete(int id)
		{
			var offer = await _context.CommercialOffers
				.Include(o => o.Items)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			var contractExists = await _context.Contracts
				.AnyAsync(c => c.CommercialOfferId == offer.Id);

			if (contractExists)
			{
				TempData["Error"] = $"Нельзя удалить КП №{offer.OfferNumber}: по нему уже создан договор.";
				return RedirectToAction("Index");
			}

			foreach (var item in offer.Items ?? new List<OfferItem>())
			{
				var product = await _context.Products.FindAsync(item.ProductId);
				if (product != null)
				{
					product.Stock += item.Quantity;
				}
			}

			var linkedDeal = await _context.Deals
				.FirstOrDefaultAsync(d => d.CommercialOfferId == offer.Id);
			if (linkedDeal != null)
			{
				linkedDeal.CommercialOfferId = null;
			}

			var deletedOfferNumber = offer.OfferNumber;
			_context.CommercialOffers.Remove(offer);
			await _context.SaveChangesAsync();
			await LogAuditAsync("DeleteOffer", $"Удалено коммерческое предложение №{deletedOfferNumber}. Товары возвращены на склад.", "CommercialOffer", id);

			TempData["Success"] = $"КП №{deletedOfferNumber} удалено. Товары возвращены на склад.";
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Policy = PermissionPolicies.OffersManage)]
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
			await LogAuditAsync("ResetOfferStatus", $"Статус КП №{offer.OfferNumber} сброшен на «Отправлено».", "CommercialOffer", offer.Id);

			TempData["Success"] = $"Статус КП №{offer.OfferNumber} сброшен на 'Отправлено'.";
			return RedirectToAction("Index");
		}
		private int? GetCurrentUserId()
		{
			var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			return int.TryParse(value, out var id) ? id : null;
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

		private async Task LogAuditAsync(string action, string description, string entityType, int? entityId)
		{
			var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			int? userId = int.TryParse(userIdValue, out var parsedId) ? parsedId : null;
			await _auditService.LogAsync(userId, User.Identity?.Name, action, description, entityType, entityId);
		}

	}
}