using ContractorHub.Data;
using ContractorHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractorHub.Controllers
{
	public class ContractsController : Controller
	{
		private readonly AppDbContext _context;

		public ContractsController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(string searchString)
		{
			var contracts = from c in _context.Contracts
							.Include(c => c.Client)
							.Include(c => c.CommercialOffer)
							select c;

			if (!string.IsNullOrEmpty(searchString))
			{
				contracts = contracts.Where(c => c.ContractNumber.Contains(searchString) ||
												 c.Client.Name.Contains(searchString));
			}

			return View(await contracts.ToListAsync());
		}

		[HttpPost]
		public async Task<IActionResult> CreateFromOffer(int offerId)
		{
			var offer = await _context.CommercialOffers
				.Include(o => o.Client)
				.FirstOrDefaultAsync(o => o.Id == offerId);

			if (offer == null)
			{
				TempData["Error"] = "КП не найдено.";
				return RedirectToAction("Index");
			}

			var existingContract = await _context.Contracts
				.FirstOrDefaultAsync(c => c.CommercialOfferId == offerId);

			if (existingContract != null)
			{
				TempData["Error"] = $"Договор №{existingContract.ContractNumber} уже создан на это КП.";
				return RedirectToAction("Index");
			}

			var contract = new Contract
			{
				ContractNumber = $"Д-{DateTime.Now:yyyyMMdd}-{offerId}",
				Date = DateTime.Now,
				ClientId = offer.ClientId,
				CommercialOfferId = offer.Id,
				TotalAmount = offer.TotalAmount,
				Status = "Черновик",
				Notes = $"Создан на основе КП №{offer.OfferNumber}"
			};

			_context.Contracts.Add(contract);
			await _context.SaveChangesAsync();

			offer.Status = "Согласовано";
			await _context.SaveChangesAsync();

			TempData["Success"] = $"Договор №{contract.ContractNumber} создан на сумму {contract.TotalAmount:N2} руб.";
			return RedirectToAction("Index");
		}
		[HttpPost]
		public async Task<IActionResult> ChangeStatus(int id, string status)
		{
			var contract = await _context.Contracts.FindAsync(id);
			if (contract == null)
			{
				TempData["Error"] = "Договор не найден.";
				return RedirectToAction("Index");
			}

			contract.Status = status;

			if (status == "Активен" && contract.SignedDate == null)
			{
				contract.SignedDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			TempData["Success"] = $"Статус договора №{contract.ContractNumber} изменён на '{status}'";
			return RedirectToAction("Index");
		}
		[HttpGet]
		public async Task<IActionResult> DownloadContract(int id)
		{
			var contract = await _context.Contracts
				.Include(c => c.Client)
				.Include(c => c.CommercialOffer)
					.ThenInclude(o => o.Items)
						.ThenInclude(i => i.Product)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (contract == null)
			{
				TempData["Error"] = "Договор не найден.";
				return RedirectToAction("Index");
			}

			var document = Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(2, Unit.Centimetre);
					page.DefaultTextStyle(TextStyle.Default.FontSize(12));

					page.Header()
						.Text($"ДОГОВОР №{contract.ContractNumber}")
						.FontSize(18)
						.Bold()
						.AlignCenter()
						.FontColor(Colors.Blue.Darken2);

					page.Content()
						.PaddingVertical(1, Unit.Centimetre)
						.Column(col =>
						{
							col.Item().Text($"Клиент: {contract.Client?.Name ?? "Не указан"}");
							col.Item().Text($"ИНН: {contract.Client?.Inn ?? "—"}");
							col.Item().Text($"Контактное лицо: {contract.Client?.ContactPerson ?? "—"}");
							col.Item().Text($"Телефон: {contract.Client?.Phone ?? "—"}");
							col.Item().Text($"Email: {contract.Client?.Email ?? "—"}");
							col.Item().Text($"Дата договора: {contract.Date.ToShortDateString()}");
							col.Item().Text($"Статус: {contract.Status}");
							col.Item().Text($"Основание: КП №{contract.CommercialOffer?.OfferNumber ?? "—"}");

							col.Item().PaddingTop(1, Unit.Centimetre);
							col.Item().Text("ПЕРЕЧЕНЬ ТОВАРОВ:").Bold().FontSize(14);

							col.Item().Table(table =>
							{
								table.ColumnsDefinition(columns =>
								{
									columns.RelativeColumn(4);
									columns.RelativeColumn(1);
									columns.RelativeColumn(1);
									columns.RelativeColumn(1);
								});

								table.Header(header =>
								{
									header.Cell().Text("Наименование").Bold();
									header.Cell().Text("Цена (₽)").Bold().AlignRight();
									header.Cell().Text("Кол-во").Bold().AlignCenter();
									header.Cell().Text("Сумма (₽)").Bold().AlignRight();
								});

								if (contract.CommercialOffer?.Items != null)
								{
									foreach (var item in contract.CommercialOffer.Items)
									{
										table.Cell().Text(item.Product?.Name ?? "—");
										table.Cell().Text(item.Price.ToString("N2")).AlignRight();
										table.Cell().Text(item.Quantity.ToString()).AlignCenter();
										table.Cell().Text((item.Price * item.Quantity).ToString("N2")).AlignRight();
									}
								}

								table.Cell().ColumnSpan(3).Text("ИТОГО:").Bold().AlignRight();
								table.Cell().Text(contract.TotalAmount.ToString("N2")).Bold().AlignRight();
							});

							col.Item().PaddingTop(2, Unit.Centimetre);
							col.Item().Row(row =>
							{
								row.RelativeItem().Text("______________________\n(Руководитель)").AlignLeft();
								row.RelativeItem().Text("______________________\n(Клиент)").AlignRight();
							});

							col.Item().PaddingTop(1, Unit.Centimetre);
							col.Item().Text($"Сгенерировано в Contractor Hub {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
						});

					page.Footer()
						.AlignCenter()
						.Text("ООО «Contractor Hub» • www.contractorhub.ru • Все права защищены")
						.FontSize(10)
						.FontColor(Colors.Grey.Medium);
				});
			});

			var pdfBytes = document.GeneratePdf();

			return File(pdfBytes, "application/pdf", $"Договор_{contract.ContractNumber}_{DateTime.Now:yyyyMMdd}.pdf");
		}
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var contract = await _context.Contracts
				.Include(c => c.CommercialOffer)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (contract == null)
			{
				TempData["Error"] = "Договор не найден.";
				return RedirectToAction("Index");
			}

			if (contract.Status != "Закрыт")
			{
				TempData["Error"] = $"Нельзя удалить договор со статусом '{contract.Status}'. Только 'Закрыт'.";
				return RedirectToAction("Index");
			}

			if (contract.CommercialOffer != null)
			{
				contract.CommercialOffer.Status = "Отправлено"; 
			}

			_context.Contracts.Remove(contract);
			await _context.SaveChangesAsync();

			TempData["Success"] = $"Договор №{contract.ContractNumber} удалён. Статус КП восстановлен.";
			return RedirectToAction("Index");
		}
	}
}