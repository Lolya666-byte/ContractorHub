namespace ContractorHub.Services
{
    public static class Permissions
    {
        public const string DashboardView = "dashboard.view";
        public const string ClientsView = "clients.view";
        public const string ClientsManage = "clients.manage";
        public const string ProductsView = "products.view";
        public const string ProductsManage = "products.manage";
        public const string OffersView = "offers.view";
        public const string OffersManage = "offers.manage";
        public const string ContractsView = "contracts.view";
        public const string ContractsManage = "contracts.manage";
        public const string DealsView = "deals.view";
        public const string DealsManage = "deals.manage";
        public const string UsersManage = "users.manage";
        public const string AuditView = "audit.view";
    }
}

namespace ContractorHub.Services
{
    public static class PermissionPolicies
    {
        public const string DashboardView = PermissionPolicyProvider.Prefix + Permissions.DashboardView;
        public const string ClientsView = PermissionPolicyProvider.Prefix + Permissions.ClientsView;
        public const string ClientsManage = PermissionPolicyProvider.Prefix + Permissions.ClientsManage;
        public const string ProductsView = PermissionPolicyProvider.Prefix + Permissions.ProductsView;
        public const string ProductsManage = PermissionPolicyProvider.Prefix + Permissions.ProductsManage;
        public const string OffersView = PermissionPolicyProvider.Prefix + Permissions.OffersView;
        public const string OffersManage = PermissionPolicyProvider.Prefix + Permissions.OffersManage;
        public const string ContractsView = PermissionPolicyProvider.Prefix + Permissions.ContractsView;
        public const string ContractsManage = PermissionPolicyProvider.Prefix + Permissions.ContractsManage;
        public const string DealsView = PermissionPolicyProvider.Prefix + Permissions.DealsView;
        public const string DealsManage = PermissionPolicyProvider.Prefix + Permissions.DealsManage;
        public const string UsersManage = PermissionPolicyProvider.Prefix + Permissions.UsersManage;
        public const string AuditView = PermissionPolicyProvider.Prefix + Permissions.AuditView;
    }
}
