using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;
using ContractorHub.Models;

namespace ContractorHub.Controllers
{
	public class ClientsController : Controller
	{
		private readonly AppDbContext _context;

		public ClientsController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(string searchString)
		{
			var clients = from c in _context.Clients select c;

			if (!string.IsNullOrEmpty(searchString))
			{
				clients = clients.Where(c => c.Name.Contains(searchString) ||
											 c.Inn.Contains(searchString) ||
											 c.ContactPerson.Contains(searchString) ||
											 c.Phone.Contains(searchString) ||
											 c.Email.Contains(searchString));
			}

			return View(await clients.ToListAsync());
		}

		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Create(Client client)
		{
			if (ModelState.IsValid)
			{
				if (!string.IsNullOrEmpty(client.Inn))
				{
					var exists = await _context.Clients.AnyAsync(c => c.Inn == client.Inn);
					if (exists)
					{
						ModelState.AddModelError("Inn", "Клиент с таким ИНН уже существует");
						return View(client);
					}
				}

				client.CreatedAt = DateTime.Now;
				_context.Clients.Add(client);
				await _context.SaveChangesAsync();
				TempData["Success"] = $"Клиент '{client.Name}' добавлен!";
				return RedirectToAction("Index");
			}
			return View(client);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var client = await _context.Clients.FindAsync(id);
			if (client == null)
			{
				TempData["Error"] = "Клиент не найден.";
				return RedirectToAction("Index");
			}
			return View(client);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(int id, Client client)
		{
			if (id != client.Id)
			{
				TempData["Error"] = "Ошибка идентификации клиента.";
				return RedirectToAction("Index");
			}

			if (ModelState.IsValid)
			{
				if (!string.IsNullOrEmpty(client.Inn))
				{
					var exists = await _context.Clients.AnyAsync(c => c.Inn == client.Inn && c.Id != client.Id);
					if (exists)
					{
						ModelState.AddModelError("Inn", "Клиент с таким ИНН уже существует");
						return View(client);
					}
				}

				var existingClient = await _context.Clients.FindAsync(id);
				if (existingClient == null)
				{
					TempData["Error"] = "Клиент не найден.";
					return RedirectToAction("Index");
				}

				existingClient.Name = client.Name;
				existingClient.Inn = client.Inn;
				existingClient.ContactPerson = client.ContactPerson;
				existingClient.Phone = client.Phone;
				existingClient.Email = client.Email;
				existingClient.Address = client.Address;

				await _context.SaveChangesAsync();
				TempData["Success"] = $"Клиент '{client.Name}' обновлён!";
				return RedirectToAction("Index");
			}
			return View(client);
		}

		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var client = await _context.Clients.FindAsync(id);
			if (client != null)
			{
				_context.Clients.Remove(client);
				await _context.SaveChangesAsync();
				TempData["Success"] = $"Клиент '{client.Name}' удалён!";
			}
			return RedirectToAction("Index");
		}
	}
}