using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class OfferItem
	{
		[Key]
		public int Id { get; set; }
		public int CommercialOfferId { get; set; }
		public CommercialOffer? CommercialOffer { get; set; }
		public int ProductId { get; set; }
		public Product? Product { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; } 
		public decimal Total => Quantity * Price;
	}
}