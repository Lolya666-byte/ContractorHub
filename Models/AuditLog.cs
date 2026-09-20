using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = "Система";

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
