using ContractorHub.Data;
using ContractorHub.Models;
using ContractorHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContractorHub.Controllers
{
    [Authorize(Policy = PermissionPolicies.DealsView)]
    public class DealsController : Controller
    {
        private static readonly string[] Statuses =
        {
            "Новая", "В работе", "КП подготовлено", "КП отправлено",
            "На согласовании", "Договор", "Успешна", "Закрыта", "Отменена"
        };

        private readonly AppDbContext _context;
        public DealsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? searchString)
        {
            var query = _context.Deals
                .Include(x => x.Client)
                .Include(x => x.ResponsibleUser)
                .Include(x => x.CommercialOffer)
                .Include(x => x.Contract)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
                query = query.Where(x => x.DealNumber.Contains(searchString) || x.Client!.Name.Contains(searchString));

            return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var deal = await _context.Deals
                .Include(x => x.Client)
                .Include(x => x.ResponsibleUser)
                .Include(x => x.CommercialOffer)
                .Include(x => x.Contract)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (deal == null) return NotFound();

            ViewBag.History = await _context.DealHistories
                .Include(x => x.ChangedByUser)
                .Where(x => x.DealId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            ViewBag.Statuses = Statuses;
            return View(deal);
        }

        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> Create()
        {
            await FillLists();
            return View(new Deal { DealNumber = $"С-{DateTime.UtcNow:yyyyMMdd-HHmm}", Status = "Новая" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> Create(Deal deal)
        {
            RemoveNavigationValidation();
            if (!ModelState.IsValid)
            {
                await FillLists();
                return View(deal);
            }

            deal.Status = NormalizeStatus(deal.Status);
            if (deal.CommercialOfferId.HasValue)
            {
                var offer = await _context.CommercialOffers.FindAsync(deal.CommercialOfferId.Value);
                if (offer == null)
                {
                    ModelState.AddModelError(nameof(Deal.CommercialOfferId), "Выбранное КП не найдено.");
                    await FillLists();
                    return View(deal);
                }

                deal.Amount = offer.TotalAmount;
            }
            deal.CreatedAt = DateTime.UtcNow;
            deal.NextActionAt = ToUtc(deal.NextActionAt);
            ApplyNextActionDefaults(deal);
            deal.ClosedAt = deal.Status is "Успешна" or "Закрыта" ? DateTime.UtcNow : null;
            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();
            await AddHistory(deal, null, deal.Status, "Сделка создана.");

            TempData["Success"] = $"Сделка {deal.DealNumber} создана.";
            return RedirectToAction(nameof(Details), new { id = deal.Id });
        }

        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> Edit(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null) return NotFound();
            await FillLists();
            return View(deal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> Edit(int id, Deal model)
        {
            if (id != model.Id) return BadRequest();
            RemoveNavigationValidation();
            if (!ModelState.IsValid)
            {
                await FillLists();
                return View(model);
            }

            var deal = await _context.Deals.FindAsync(id);
            if (deal == null) return NotFound();

            var oldStatus = deal.Status;
            deal.DealNumber = model.DealNumber;
            deal.ClientId = model.ClientId;
            deal.ResponsibleUserId = model.ResponsibleUserId;
            deal.CommercialOfferId = model.CommercialOfferId;
            deal.ContractId = model.ContractId;
            if (model.CommercialOfferId.HasValue)
            {
                var offer = await _context.CommercialOffers.FindAsync(model.CommercialOfferId.Value);
                if (offer == null)
                {
                    ModelState.AddModelError(nameof(Deal.CommercialOfferId), "Выбранное КП не найдено.");
                    await FillLists();
                    return View(model);
                }

                deal.Amount = offer.TotalAmount;
            }
            else
            {
                deal.Amount = model.Amount;
            }
            deal.Status = NormalizeStatus(model.Status);
            deal.Notes = model.Notes;
            deal.NextAction = model.NextAction;
            deal.NextActionAt = ToUtc(model.NextActionAt);
            deal.ClosedAt = deal.Status is "Успешна" or "Закрыта" ? (deal.ClosedAt ?? DateTime.UtcNow) : null;

            await _context.SaveChangesAsync();
            if (oldStatus != deal.Status)
                await AddHistory(deal, oldStatus, deal.Status, "Этап изменён при редактировании сделки.");

            TempData["Success"] = $"Сделка {deal.DealNumber} обновлена.";
            return RedirectToAction(nameof(Details), new { id = deal.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> ChangeStatus(int id, string status, string? comment)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null) return NotFound();

            status = NormalizeStatus(status);
            var oldStatus = deal.Status;
            if (oldStatus == status)
            {
                TempData["Error"] = "Сделка уже находится на этом этапе.";
                return RedirectToAction(nameof(Details), new { id });
            }

            deal.Status = status;
            deal.ClosedAt = status is "Успешна" or "Закрыта" ? (deal.ClosedAt ?? DateTime.UtcNow) : null;
            ApplyNextActionDefaults(deal, true);
            await _context.SaveChangesAsync();
            await AddHistory(deal, oldStatus, status, comment);

            TempData["Success"] = $"Этап сделки изменён: {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> CreateOffer(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null) return NotFound();

            if (deal.CommercialOfferId.HasValue)
            {
                return RedirectToAction("Edit", "CommercialOffers", new { id = deal.CommercialOfferId.Value });
            }

            var offer = new CommercialOffer
            {
                OfferNumber = GenerateOfferNumber(),
                ClientId = deal.ClientId,
                Date = DateTime.UtcNow,
                Status = "Черновик",
                TotalAmount = 0
            };
            _context.CommercialOffers.Add(offer);
            await _context.SaveChangesAsync();

            deal.CommercialOfferId = offer.Id;
            var oldStatus = deal.Status;
            if (deal.Status is "Новая" or "В работе") deal.Status = "КП подготовлено";
            deal.NextAction = "Проверить КП и отправить клиенту";
            deal.NextActionAt = DateTime.UtcNow.AddDays(1);
            await _context.SaveChangesAsync();
            await AddHistory(deal, oldStatus, deal.Status, $"Создано КП №{offer.OfferNumber}.");

            TempData["Success"] = $"КП №{offer.OfferNumber} создано. Добавьте товары и сохраните его.";
            return RedirectToAction("Edit", "CommercialOffers", new { id = offer.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> CreateContract(int id)
        {
            var deal = await _context.Deals
                .Include(x => x.CommercialOffer)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (deal == null) return NotFound();

            if (deal.ContractId.HasValue)
                return RedirectToAction("Index", "Contracts");

            if (!deal.CommercialOfferId.HasValue || deal.CommercialOffer == null)
            {
                TempData["Error"] = "Сначала создайте коммерческое предложение для сделки.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var existing = await _context.Contracts.FirstOrDefaultAsync(x => x.CommercialOfferId == deal.CommercialOfferId.Value);
            if (existing != null)
            {
                deal.ContractId = existing.Id;
                deal.Status = existing.Status == "Активен" ? "Успешна" : deal.Status;
                if (deal.Status == "Успешна")
                {
                    deal.NextAction = null;
                    deal.NextActionAt = null;
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Contracts");
            }

            var contract = new Contract
            {
                ContractNumber = $"Д-{DateTime.UtcNow:yyyyMMdd}-{deal.Id}",
                Date = DateTime.UtcNow,
                ClientId = deal.ClientId,
                CommercialOfferId = deal.CommercialOfferId,
                TotalAmount = deal.CommercialOffer.TotalAmount,
                Status = "Черновик",
                Notes = $"Создан из сделки {deal.DealNumber} на основании КП №{deal.CommercialOffer.OfferNumber}"
            };
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            deal.ContractId = contract.Id;
            deal.CommercialOffer.Status = "Согласовано";
            var oldStatus = deal.Status;
            deal.Status = "Договор";
            deal.NextAction = "Получить подписанный договор";
            deal.NextActionAt = DateTime.UtcNow.AddDays(3);
            await _context.SaveChangesAsync();
            await AddHistory(deal, oldStatus, deal.Status, $"Создан договор №{contract.ContractNumber}.");

            TempData["Success"] = $"Договор №{contract.ContractNumber} создан.";
            return RedirectToAction("Index", "Contracts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionPolicies.DealsManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null) return RedirectToAction(nameof(Index));
            _context.Deals.Remove(deal);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Сделка {deal.DealNumber} удалена.";
            return RedirectToAction(nameof(Index));
        }

        private void RemoveNavigationValidation()
        {
            ModelState.Remove(nameof(Deal.Client));
            ModelState.Remove(nameof(Deal.ResponsibleUser));
            ModelState.Remove(nameof(Deal.CommercialOffer));
            ModelState.Remove(nameof(Deal.Contract));
        }

        private async Task FillLists()
        {
            ViewBag.Clients = await _context.Clients.OrderBy(x => x.Name).ToListAsync();
            ViewBag.Users = await _context.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
            ViewBag.Offers = await _context.CommercialOffers.OrderByDescending(x => x.Date).ToListAsync();
            ViewBag.Contracts = await _context.Contracts.OrderByDescending(x => x.Date).ToListAsync();
            ViewBag.Statuses = Statuses;
        }

        private async Task AddHistory(Deal deal, string? fromStatus, string toStatus, string? comment)
        {
            int? userId = null;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) userId = id;
            _context.DealHistories.Add(new DealHistory
            {
                DealId = deal.Id,
                FromStatus = fromStatus ?? "—",
                ToStatus = toStatus,
                Comment = comment,
                ChangedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private static DateTime? ToUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            if (value.Value.Kind == DateTimeKind.Utc) return value.Value;
            return DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime();
        }

        private static void ApplyNextActionDefaults(Deal deal, bool force = false)
        {
            if (deal.Status is "Успешна" or "Закрыта" or "Отменена")
            {
                deal.NextAction = null;
                deal.NextActionAt = null;
                return;
            }

            if (!force && !string.IsNullOrWhiteSpace(deal.NextAction)) return;

            deal.NextAction = deal.Status switch
            {
                "Новая" => "Связаться с клиентом и определить следующий шаг",
                "В работе" => "Подготовить коммерческое предложение",
                "КП подготовлено" => "Проверить КП и отправить клиенту",
                "КП отправлено" => "Связаться с клиентом по КП",
                "На согласовании" => "Уточнить результат согласования",
                "Договор" => "Получить подписанный договор",
                _ => null
            };

            if (deal.NextAction != null && !deal.NextActionAt.HasValue)
                deal.NextActionAt = DateTime.UtcNow.AddDays(1);
        }

        private static string NormalizeStatus(string status) =>
            Statuses.Contains(status) ? status : "Новая";

        private string GenerateOfferNumber()
        {
            string prefix = DateTime.Now.ToString("yyyy-MM");
            var lastOffer = _context.CommercialOffers
                .Where(o => o.OfferNumber.StartsWith(prefix))
                .OrderByDescending(o => o.OfferNumber)
                .FirstOrDefault();
            if (lastOffer == null) return $"{prefix}-001";
            var last = lastOffer.OfferNumber[(lastOffer.OfferNumber.LastIndexOf('-') + 1)..];
            return int.TryParse(last, out var n) ? $"{prefix}-{n + 1:D3}" : $"{prefix}-001";
        }
    }
}
