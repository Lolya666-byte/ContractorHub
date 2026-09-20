using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class CommercialOffer
	{
		[Key]
		public int Id { get; set; }
		public string OfferNumber { get; set; } = string.Empty;
		public DateTime Date { get; set; } = DateTime.UtcNow;
		public int ClientId { get; set; }
		public Client? Client { get; set; }
		public decimal TotalAmount { get; set; }
		public string Status { get; set; } = "Черновик"; 
		public List<OfferItem>? Items { get; set; }
	}
}