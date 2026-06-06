using System.Threading;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IAiInsightsService
    {
        Task<CallInsightsResult> ExtractInsightsAsync(Guid call_id,string transcript, CancellationToken ct = default);

        //Task<bool> ShouldEndCallAsync(string transcriptText, CancellationToken ct);

        //Task EndCallAsync(Call call);
    }
}
