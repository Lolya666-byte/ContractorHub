using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
    public class DealHistory
    {
        [Key]
        public int Id { get; set; }

        public int DealId { get; set; }
        public Deal? Deal { get; set; }

        [Required, StringLength(50)]
        public string? FromStatus { get; set; }

        [Required, StringLength(50)]
        public string ToStatus { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Comment { get; set; }

        public int? ChangedByUserId { get; set; }
        public User? ChangedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
