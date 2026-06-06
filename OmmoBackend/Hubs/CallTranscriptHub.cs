using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OmmoBackend.Hubs
{
    [AllowAnonymous]
    public class CallTranscriptHub : Hub
    {
        public async Task JoinCallRoom(string callId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, callId);
        }

        public async Task LeaveCallRoom(string callId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
        }
    }
}
