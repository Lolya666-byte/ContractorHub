using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class User
	{
		public int Id { get; set; }

		[Required]
		[StringLength(100)]
		public string FullName { get; set; } = string.Empty;

		[Required]
		[StringLength(100)]
		public string Username { get; set; } = string.Empty;

		[Required]
		public string PasswordHash { get; set; } = string.Empty;

		[Required]
		[StringLength(50)]
		public string Role { get; set; } = "Manager";

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}