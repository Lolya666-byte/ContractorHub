using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
    public class Deal
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Номер сделки")]
        public string DealNumber { get; set; } = string.Empty;

        [Required]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public int? ResponsibleUserId { get; set; }
        public User? ResponsibleUser { get; set; }

        public int? CommercialOfferId { get; set; }
        public CommercialOffer? CommercialOffer { get; set; }

        public int? ContractId { get; set; }
        public Contract? Contract { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Сумма")]
        public decimal Amount { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Новая";

        [StringLength(1000)]
        [Display(Name = "Комментарий")]
        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "Следующее действие")]
        public string? NextAction { get; set; }

        [Display(Name = "Дата следующего действия")]
        public DateTime? NextActionAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
}
