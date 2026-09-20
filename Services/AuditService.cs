
using ContractorHub.Data;
using ContractorHub.Models;
using Microsoft.Extensions.Logging;

namespace ContractorHub.Services
{
	public class AuditService
	{
		private readonly AppDbContext _context;
		private readonly ILogger<AuditService> _logger;

		public AuditService(
			AppDbContext context,
			ILogger<AuditService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task LogAsync(
			int? userId,
			string? userName,
			string action,
			string description,
			string? entityType = null,
			int? entityId = null)
		{
			_logger.LogInformation(
				"Audit: начинаем запись. UserId={UserId}, Action={Action}",
				userId,
				action);

			try
			{
				var log = new AuditLog
				{
					UserId = userId,
					UserName = string.IsNullOrWhiteSpace(userName)
						? "Система"
						: userName,
					Action = action,
					Description = description,
					EntityType = entityType,
					EntityId = entityId,
					CreatedAt = DateTime.UtcNow
				};

				_context.AuditLogs.Add(log);

				var affected = await _context.SaveChangesAsync();

				_logger.LogInformation(
					"Audit: запись сохранена. Id={Id}, Affected={Affected}",
					log.Id,
					affected);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Audit: ошибка сохранения. UserId={UserId}, Action={Action}",
					userId,
					action);

				throw;
			}
		}
	}
}