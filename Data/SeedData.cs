using ContractorHub.Models;

namespace ContractorHub.Data
{
	public static class SeedData
	{
		public static void Initialize(AppDbContext context)
		{
			if (context.Clients.Any())
			{
				return; 
			}

			var clients = new Client[]
			{
				new Client
				{
					Name = "ООО Ромашка",
					Inn = "7701234567",
					ContactPerson = "Иванова Мария",
					Phone = "+7-999-123-45-67",
					Email = "romashka@mail.ru",
					Address = "г. Москва, ул. Тверская, д. 1"
				},
				new Client
				{
					Name = "ИП Петров",
					Inn = "7709876543",
					ContactPerson = "Петров Сергей",
					Phone = "+7-999-765-43-21",
					Email = "petrov@bk.ru",
					Address = "г. Москва, ул. Арбат, д. 10"
				},
				new Client
				{
					Name = "ООО ТехноСервис",
					Inn = "7705555555",
					ContactPerson = "Сидорова Анна",
					Phone = "+7-999-111-22-33",
					Email = "info@techno.ru",
					Address = "г. Санкт-Петербург, Невский пр., д. 20"
				}
			};
			context.Clients.AddRange(clients);
			context.SaveChanges();

			var products = new Product[]
			{
				new Product
				{
					Name = "Ноутбук Lenovo ThinkPad X1",
					Category = "Электроника",
					Price = 85000m,
					Stock = 15,
					Description = "Бизнес-ноутбук с процессором Intel Core i7, 16 ГБ ОЗУ, SSD 512 ГБ"
				},
				new Product
				{
					Name = "Принтер HP LaserJet Pro",
					Category = "Оргтехника",
					Price = 22000m,
					Stock = 8,
					Description = "Лазерный принтер для офиса, формат А4, двусторонняя печать"
				},
				new Product
				{
					Name = "Офисное кресло Ergohuman",
					Category = "Мебель",
					Price = 34000m,
					Stock = 12,
					Description = "Эргономичное кресло с регулировкой высоты и наклона спинки"
				},
				new Product
				{
					Name = "МФУ Kyocera ECOSYS M2635dn",
					Category = "Оргтехника",
					Price = 42000m,
					Stock = 5,
					Description = "Многофункциональное устройство (печать, сканирование, копирование) для малого офиса"
				},
				new Product
				{
					Name = "Стол офисный Комфорт-2",
					Category = "Мебель",
					Price = 18000m,
					Stock = 20,
					Description = "Прямой офисный стол с тумбой, ЛДСП, размер 1200x600 мм"
				}
			};
			context.Products.AddRange(products);
			context.SaveChanges();

			var offer = new CommercialOffer
			{
				OfferNumber = "КП-001",
				Date = DateTime.Now.AddDays(-3),
				ClientId = clients[0].Id, 
				Status = "Отправлено",
				TotalAmount = 0 
			};
			context.CommercialOffers.Add(offer);
			context.SaveChanges();

			var offerItems = new OfferItem[]
			{
				new OfferItem
				{
					CommercialOfferId = offer.Id,
					ProductId = products[0].Id, 
                    Quantity = 2,
					Price = products[0].Price
				},
				new OfferItem
				{
					CommercialOfferId = offer.Id,
					ProductId = products[1].Id, 
                    Quantity = 1,
					Price = products[1].Price
				}
			};
			context.OfferItems.AddRange(offerItems);
			context.SaveChanges();

			offer.TotalAmount = offerItems.Sum(i => i.Quantity * i.Price);
			context.SaveChanges();
		}
	}
}