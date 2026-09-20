using ContractorHub.Models;
using ContractorHub.Services;

namespace ContractorHub.Data
{
    public static class PermissionSeedData
    {
        private static readonly (string Code, string Name, string Category)[] Definitions =
        {
            (Permissions.DashboardView, "Просмотр рабочего стола", "Рабочий стол"),
            (Permissions.ClientsView, "Просмотр клиентов", "Клиенты"),
            (Permissions.ClientsManage, "Управление клиентами", "Клиенты"),
            (Permissions.ProductsView, "Просмотр товаров", "Товары"),
            (Permissions.ProductsManage, "Управление товарами", "Товары"),
            (Permissions.OffersView, "Просмотр коммерческих предложений", "Коммерческие предложения"),
            (Permissions.OffersManage, "Управление коммерческими предложениями", "Коммерческие предложения"),
            (Permissions.ContractsView, "Просмотр договоров", "Договоры"),
            (Permissions.ContractsManage, "Управление договорами", "Договоры"),
            (Permissions.DealsView, "Просмотр сделок", "Сделки"),
            (Permissions.DealsManage, "Управление сделками", "Сделки"),
            (Permissions.UsersManage, "Управление сотрудниками", "Сотрудники"),
            (Permissions.AuditView, "Просмотр журнала активности", "Администрирование")
        };

        private static readonly Dictionary<string, string[]> RolePermissions = new()
        {
            ["Administrator"] = Definitions.Select(x => x.Code).ToArray(),
            ["Manager"] = new[]
            {
                Permissions.DashboardView,
                Permissions.ClientsView, Permissions.ClientsManage,
                Permissions.ProductsView, Permissions.ProductsManage,
                Permissions.OffersView, Permissions.OffersManage,
                Permissions.ContractsView, Permissions.ContractsManage,
                Permissions.DealsView, Permissions.DealsManage
            },
            ["Accountant"] = new[]
            {
                Permissions.DashboardView,
                Permissions.ClientsView,
                Permissions.ContractsView,
                Permissions.DealsView
            }
        };

        public static void Initialize(AppDbContext context)
        {
            foreach (var definition in Definitions)
            {
                if (!context.Permissions.Any(x => x.Code == definition.Code))
                {
                    context.Permissions.Add(new Permission
                    {
                        Code = definition.Code,
                        Name = definition.Name,
                        Category = definition.Category
                    });
                }
            }

            context.SaveChanges();
            var permissions = context.Permissions.ToDictionary(x => x.Code);

            foreach (var role in RolePermissions)
            {
                foreach (var code in role.Value)
                {
                    if (!permissions.TryGetValue(code, out var permission)) continue;

                    if (!context.RolePermissions.Any(x => x.Role == role.Key && x.PermissionId == permission.Id))
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            Role = role.Key,
                            PermissionId = permission.Id
                        });
                    }
                }
            }

            context.SaveChanges();
        }
    }
}
