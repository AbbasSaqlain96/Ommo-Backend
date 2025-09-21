using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        public NotificationHub(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task JoinGroup(string companyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, companyId);
        }

        public async Task LeaveGroup(string companyId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, companyId);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var companyIdStr = httpContext?.User.FindFirst("Company_ID")?.Value;

            if (!string.IsNullOrEmpty(companyIdStr) && int.TryParse(companyIdStr, out int companyId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, companyIdStr);

                var notifications = await _notificationService.GetRecentNotificationsAsync(companyId);
                await Clients.Caller.SendAsync("ReceiveNotificationHistory", notifications);
            }

            await base.OnConnectedAsync();
        }
    }
}
