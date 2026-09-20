using System.Security.Claims;
using ContractorHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractorHub.Services
{
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string permission) => Permission = permission;
        public string Permission { get; }
    }

    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly AppDbContext _context;
        public PermissionAuthorizationHandler(AppDbContext context) => _context = context;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var role = context.User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(role)) return;

            var allowed = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(x => x.Role == role && x.Permission.Code == requirement.Permission);

            if (allowed) context.Succeed(requirement);
        }
    }

    public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public const string Prefix = "Permission:";

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return base.GetPolicyAsync(policyName);

            var permission = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }

    public static class PermissionPolicy
    {
        public static string For(string permission) => PermissionPolicyProvider.Prefix + permission;
    }
}
