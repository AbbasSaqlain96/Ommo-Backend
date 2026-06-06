using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

namespace OmmoBackend.Hubs
{
    public class UserChatHub : Hub
    {

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        // Called by frontend after connection
        public async Task JoinUserGroup(int userId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"user-{userId}"
            );
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
