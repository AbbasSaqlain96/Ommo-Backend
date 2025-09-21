using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class CallTranscriptService : ICallTranscriptService
    {
        private readonly ICallRepository _callRepo;
        private readonly ITranscriptRepository _transcriptRepo;

        public CallTranscriptService(ICallRepository callRepo, ITranscriptRepository transcriptRepo)
        {
            _callRepo = callRepo;
            _transcriptRepo = transcriptRepo;
        }

        public async Task<ServiceResponse<List<CallTranscriptLineDto>>> GetTranscriptAsync(int callId, int companyId)
        {
            var call = await _callRepo.GetByIdAsync(callId);
            if (call == null)
                return ServiceResponse<List<CallTranscriptLineDto>>.ErrorResponse("Call not found.", 404);

            if (call.company_id != companyId)
                return ServiceResponse<List<CallTranscriptLineDto>>.ErrorResponse("Forbidden: This call does not belong to your company.", 403);

            var transcripts = await _transcriptRepo.GetByCallIdAsync(callId);

            var result = transcripts
                .OrderBy(t => t.timestamp)
                .Select(t => new CallTranscriptLineDto
                {
                    Speaker = t.speaker,
                    Text = t.text,
                    Timestamp = t.timestamp
                }).ToList();

            return ServiceResponse<List<CallTranscriptLineDto>>.SuccessResponse(result);
        }
    }
}
