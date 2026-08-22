namespace ContractorHub.Models
{
	public class CommercialOfferViewModel
	{
		public string OfferNumber { get; set; } = string.Empty;
		public int ClientId { get; set; }
		public List<OfferItemViewModel> Items { get; set; } = new List<OfferItemViewModel>();
	}

	public class OfferItemViewModel
	{
		public int ProductId { get; set; }
		public int Quantity { get; set; }
	}
}