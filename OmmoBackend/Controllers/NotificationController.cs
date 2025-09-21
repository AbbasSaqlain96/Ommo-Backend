using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Implementations;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        public NotificationController(AppDbContext dbContext, INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        [RequireAuthenticationOnlyAttribute]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var companyIdStr = User.FindFirst("Company_ID")?.Value;
                if (!int.TryParse(companyIdStr, out int companyId))
                {
                    _logger.LogWarning("Invalid Company ID in user claims.");
                    return BadRequest("Invalid Company ID.");
                }

                _logger.LogInformation("Fetching notifications for Company ID: {CompanyId}", companyId);
                var notifications = await _notificationService.GetNotificationsAsync(companyId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving notifications.");
                return StatusCode(500, "An error occurred while fetching notifications.");
            }
        }
    }
}
