using ContractorHub.Services;
using ContractorHub.Data;
using Microsoft.AspNetCore.Authorization;
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
		private readonly AuditService _auditService;

		public ContractsController(AppDbContext context, AuditService auditService)
		{
			_context = context;
			_auditService = auditService;
		}

		[Authorize(Policy = PermissionPolicies.ContractsView)]
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
		[ValidateAntiForgeryToken]
		[Authorize(Policy = PermissionPolicies.ContractsManage)]
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
				ContractNumber = $"Д-{DateTime.UtcNow:yyyyMMdd}-{offerId}",
				Date = DateTime.UtcNow,
				ClientId = offer.ClientId,
				CommercialOfferId = offer.Id,
				TotalAmount = offer.TotalAmount,
				Status = "Черновик",
				Notes = $"Создан на основе КП №{offer.OfferNumber}"
			};

			_context.Contracts.Add(contract);
			await _context.SaveChangesAsync();

			offer.Status = "Согласовано";
			var linkedDeal = await _context.Deals.FirstOrDefaultAsync(d => d.CommercialOfferId == offer.Id);
			if (linkedDeal != null)
			{
				linkedDeal.ContractId = contract.Id;
				if (linkedDeal.Status != "Успешна" && linkedDeal.Status != "Закрыта" && linkedDeal.Status != "Отменена")
				{
					var oldStatus = linkedDeal.Status;
					linkedDeal.Status = "Договор";
					linkedDeal.NextAction = "Получить подписанный договор";
					linkedDeal.NextActionAt = DateTime.UtcNow.AddDays(3);
					_context.DealHistories.Add(new DealHistory { DealId = linkedDeal.Id, FromStatus = oldStatus, ToStatus = linkedDeal.Status, Comment = $"Создан договор №{contract.ContractNumber}.", ChangedByUserId = GetCurrentUserId(), CreatedAt = DateTime.UtcNow });
				}
			}
			await _context.SaveChangesAsync();
			await LogAuditAsync("CreateContract", $"Создан договор №{contract.ContractNumber} на сумму {contract.TotalAmount:N2} руб. на основе КП №{offer.OfferNumber}.", "Contract", contract.Id);

			TempData["Success"] = $"Договор №{contract.ContractNumber} создан на сумму {contract.TotalAmount:N2} руб.";
			return RedirectToAction("Index");
		}
		[HttpPost]
		[Authorize(Policy = PermissionPolicies.ContractsManage)]
		public async Task<IActionResult> ChangeStatus(int id, string status)
		{
			var contract = await _context.Contracts.FindAsync(id);
			if (contract == null)
			{
				TempData["Error"] = "Договор не найден.";
				return RedirectToAction("Index");
			}

			contract.Status = status;

			var linkedDeal = await _context.Deals.FirstOrDefaultAsync(d => d.ContractId == contract.Id);
			if (linkedDeal != null && linkedDeal.Status != "Закрыта" && linkedDeal.Status != "Отменена")
			{
				var oldStatus = linkedDeal.Status;
				var mappedStatus = status switch
				{
					"Активен" => "Успешна",
					"Закрыт" => "Закрыта",
					_ => "Договор"
				};
				linkedDeal.Status = mappedStatus;
				linkedDeal.NextAction = mappedStatus is "Успешна" or "Закрыта" ? null : "Получить подписанный договор";
				linkedDeal.NextActionAt = mappedStatus is "Успешна" or "Закрыта" ? null : DateTime.UtcNow.AddDays(2);
				if (oldStatus != mappedStatus)
					_context.DealHistories.Add(new DealHistory { DealId = linkedDeal.Id, FromStatus = oldStatus, ToStatus = mappedStatus, Comment = $"Статус сделки обновлён по договору №{contract.ContractNumber}.", ChangedByUserId = GetCurrentUserId(), CreatedAt = DateTime.UtcNow });
			}

			if (status == "Активен" && contract.SignedDate == null)
			{
				contract.SignedDate = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();
			await LogAuditAsync("ChangeContractStatus", $"Для договора №{contract.ContractNumber} установлен статус «{status}».", "Contract", contract.Id);
			TempData["Success"] = $"Статус договора №{contract.ContractNumber} изменён на '{status}'";
			return RedirectToAction("Index");
		}
		private int? GetCurrentUserId()
		{
			var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			return int.TryParse(value, out var id) ? id : null;
		}

		[HttpGet]
		[Authorize(Policy = PermissionPolicies.ContractsView)]
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

			await LogAuditAsync("DownloadContract", $"Скачан PDF договора №{contract.ContractNumber}.", "Contract", contract.Id);

			return File(pdfBytes, "application/pdf", $"Договор_{contract.ContractNumber}_{DateTime.Now:yyyyMMdd}.pdf");
		}
		[HttpPost]
		[Authorize(Policy = PermissionPolicies.ContractsManage)]
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

			var deletedContractNumber = contract.ContractNumber;
			_context.Contracts.Remove(contract);
			await _context.SaveChangesAsync();
			await LogAuditAsync("DeleteContract", $"Удалён договор №{deletedContractNumber}. Статус КП восстановлен.", "Contract", id);

			TempData["Success"] = $"Договор №{deletedContractNumber} удалён. Статус КП восстановлен.";
			return RedirectToAction("Index");
		}

		private async Task LogAuditAsync(string action, string description, string entityType, int? entityId)
		{
			var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			int? userId = int.TryParse(userIdValue, out var parsedId) ? parsedId : null;
			await _auditService.LogAsync(userId, User.Identity?.Name, action, description, entityType, entityId);
		}

	}
}