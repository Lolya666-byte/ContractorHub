using System.ComponentModel.DataAnnotations;

namespace ContractorHub.Models
{
	public class Client
	{
		[Key]
		public int Id { get; set; }

		[Required(ErrorMessage = "Название клиента обязательно")]
		[StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
		public string Name { get; set; } = string.Empty;

		[RegularExpression(@"^\d{10}$|^\d{12}$", ErrorMessage = "ИНН должен содержать 10 или 12 цифр")]
		[Display(Name = "ИНН")]
		public string? Inn { get; set; }

		[StringLength(100, ErrorMessage = "Контактное лицо не может быть длиннее 100 символов")]
		[Display(Name = "Контактное лицо")]
		public string? ContactPerson { get; set; }

		[RegularExpression(@"^(8\d{10}|\+7\d{10})$",
			ErrorMessage = "Введите номер в формате 89991234567 (11 цифр) или +79991234567 (12 символов)")]
		[Display(Name = "Телефон")]
		public string? Phone { get; set; }

		[EmailAddress(ErrorMessage = "Введите корректный email (например, user@domain.com)")]
		[Display(Name = "Email")]
		public string? Email { get; set; }

		[StringLength(200, ErrorMessage = "Адрес не может быть длиннее 200 символов")]
		[Display(Name = "Адрес")]
		public string? Address { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}