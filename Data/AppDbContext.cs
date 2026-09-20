using Microsoft.EntityFrameworkCore;
using ContractorHub.Models;

namespace ContractorHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CommercialOffer> CommercialOffers { get; set; }
        public DbSet<OfferItem> OfferItems { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Deal> Deals { get; set; }
        public DbSet<DealHistory> DealHistories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Permission>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasIndex(x => new { x.Role, x.PermissionId })
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
