using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class Product
	{
		[Key]
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Category { get; set; }
		public decimal Price { get; set; }
		public int? Stock { get; set; }
		public string? Description { get; set; }
	}
}