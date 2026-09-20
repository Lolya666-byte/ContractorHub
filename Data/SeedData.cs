using ContractorHub.Models;
using ContractorHub.Services;

namespace ContractorHub.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            if (!context.Users.Any())
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

                var admin = new User
                {
                    FullName = "Системный администратор",
                    Username = "admin",
                    Role = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

                context.Users.Add(admin);
                context.SaveChanges();
            }

            if (!context.Clients.Any())
            {
                context.Clients.AddRange(
                    new Client
                    {
                        Name = "ООО «Ромашка»",
                        Email = "info@romashka.ru",
                        Phone = "+7 (900) 000-00-01",
                        Address = "Москва"
                    },
                    new Client
                    {
                        Name = "ООО «Вектор»",
                        Email = "info@vector.ru",
                        Phone = "+7 (900) 000-00-02",
                        Address = "Санкт-Петербург"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
