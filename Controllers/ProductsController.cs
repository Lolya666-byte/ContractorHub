using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractorHub.Data;
using ContractorHub.Models;

namespace ContractorHub.Controllers
{
	public class ProductsController : Controller
	{
		private readonly AppDbContext _context;

		public ProductsController(AppDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(string searchString)
		{
			var products = from p in _context.Products select p;

			if (!string.IsNullOrEmpty(searchString))
			{
				products = products.Where(p => p.Name.Contains(searchString) ||
											   p.Category.Contains(searchString) ||
											   p.Description.Contains(searchString));
			}

			return View(await products.ToListAsync());
		}

		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Create(Product product)
		{
			if (ModelState.IsValid)
			{
				_context.Products.Add(product);
				await _context.SaveChangesAsync();
				TempData["Success"] = $"Товар '{product.Name}' добавлен!";
				return RedirectToAction("Index");
			}
			return View(product);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var product = await _context.Products.FindAsync(id);
			if (product == null)
			{
				TempData["Error"] = "Товар не найден.";
				return RedirectToAction("Index");
			}
			return View(product);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(int id, Product product)
		{
			if (id != product.Id)
			{
				TempData["Error"] = "Ошибка идентификации товара.";
				return RedirectToAction("Index");
			}

			if (ModelState.IsValid)
			{
				var existingProduct = await _context.Products.FindAsync(id);
				if (existingProduct == null)
				{
					TempData["Error"] = "Товар не найден.";
					return RedirectToAction("Index");
				}

				existingProduct.Name = product.Name;
				existingProduct.Category = product.Category;
				existingProduct.Price = product.Price;
				existingProduct.Stock = product.Stock;
				existingProduct.Description = product.Description;

				await _context.SaveChangesAsync();
				TempData["Success"] = $"Товар '{product.Name}' обновлён!";
				return RedirectToAction("Index");
			}
			return View(product);
		}
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var product = await _context.Products.FindAsync(id);
			if (product != null)
			{
				_context.Products.Remove(product);
				await _context.SaveChangesAsync();
				TempData["Success"] = $"Товар '{product.Name}' удалён!";
			}
			return RedirectToAction("Index");
		}

		[HttpPost]
		public async Task<IActionResult> UpdateStock(int id, int newStock)
		{
			if (newStock < 0)
			{
				TempData["Error"] = "Остаток не может быть отрицательным.";
				return RedirectToAction("Index");
			}

			var product = await _context.Products.FindAsync(id);
			if (product == null)
			{
				TempData["Error"] = "Товар не найден.";
				return RedirectToAction("Index");
			}

			product.Stock = newStock;
			await _context.SaveChangesAsync();

			TempData["Success"] = $"Остаток товара '{product.Name}' обновлён: {newStock} шт.";
			return RedirectToAction("Index");
		}
	}
}