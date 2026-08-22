using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class Contract
	{
		[Key]
		public int Id { get; set; }
		public string ContractNumber { get; set; } = string.Empty;
		public DateTime Date { get; set; } = DateTime.Now;
		public int ClientId { get; set; }
		public Client? Client { get; set; }
		public int? CommercialOfferId { get; set; }
		public CommercialOffer? CommercialOffer { get; set; }
		public decimal TotalAmount { get; set; }
		public string Status { get; set; } = "Черновик"; 
		public DateTime? SignedDate { get; set; }
		public string? Notes { get; set; }
	}
}