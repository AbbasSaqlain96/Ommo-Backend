// Services/Interfaces/ITranscriptPollingService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmmoBackend.Services.Interfaces
{
    public interface ITranscriptPollingService
    {
        Task PollAllActiveCallsAsync(CancellationToken ct = default);
        Task FetchAndBroadcastTranscriptAsync(Guid callId, CancellationToken ct = default);
    }
}
