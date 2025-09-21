using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<NotificationRepository> _logger;
        public NotificationRepository(AppDbContext dbContext, ILogger<NotificationRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(int companyId, int count)
        {
            try
            {
                _logger.LogInformation("Fetching {Count} recent notifications for Company ID: {CompanyId}", count, companyId);
                return await _dbContext.notifications
                    .Where(n => n.company_id == companyId)
                    .OrderByDescending(n => n.created_at)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent notifications for Company ID: {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsAsync(int companyId)
        {
            try
            {
                return await _dbContext.notifications
                    .Where(n => n.company_id == companyId)
                    .OrderByDescending(n => n.created_at)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications for Company ID: {CompanyId}", companyId);
                throw;
            }
        }
    }
}