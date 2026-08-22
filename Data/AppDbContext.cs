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
	}
}