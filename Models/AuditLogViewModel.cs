namespace ContractorHub.Models
{
	public class AuditLogViewModel
	{
		public int Id { get; set; }

		public string UserName { get; set; } = string.Empty;

		public string Action { get; set; } = string.Empty;

		public string? EntityType { get; set; }

		public string Description { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }
	}
}