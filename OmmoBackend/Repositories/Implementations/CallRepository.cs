using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CallRepository : GenericRepository<Call>, ICallRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CallRepository> _logger;

        public CallRepository(AppDbContext dbContext, ILogger<CallRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task UpdateStatusByTwilioSidAsync(
            string twilioSid,
            string statusOfCall,
            string? callresult,
            CancellationToken ct = default)
        {
            var call = await _dbContext.call
                .FirstOrDefaultAsync(c => c.twilio_call_sid == twilioSid, ct);

            if (call == null)
                return;

            // update status + result
            call.status_of_call = statusOfCall;
            if (callresult != null)
            {
                call.call_result = callresult;
            }

            // End states
            bool isEnded = statusOfCall == "ended";
            bool isNoAnswerOrFailed = statusOfCall == "no-answer" || statusOfCall == "failed";

            if (isEnded || isNoAnswerOrFailed)
            {
                // Only set once
                if (call.call_end_time == null)
                {
                    if (isEnded)
                    {
                        var now = DateTime.UtcNow;
                        call.call_end_time = now;
                        call.call_duration = (int)(now - call.call_timestamp).TotalSeconds;
                    }
                    else
                    {
                        // no-answer or failed
                        call.call_end_time = call.call_timestamp;
                        call.call_duration = 0;
                    }
                }
            }


            await _dbContext.SaveChangesAsync(ct);
        }


        public async Task<CallTakeoverInfo?> GetCallForTakeoverAsync(Guid callId)
        {
            return await _dbContext.call
                .Where(c => c.call_id == callId)
                .Select(c => new CallTakeoverInfo
                {
                    CallId = c.call_id,
                    CompanyId = c.company_id,
                    TwilioCallSid = c.twilio_call_sid
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<CalledLoadDto>> GetCalledLoadsAsync(int companyId)
        {
            var since = DateTime.UtcNow.AddHours(-24);

            var query = from c in _dbContext.call
                        where c.company_id == companyId
                           && c.call_timestamp >= since
                        select new CalledLoadDto
                        {
                            Source = c.loadboard_type,
                            ReferenceId = c.reference_id,
                            CalledAtUtc = c.call_timestamp
                        };

            return await query.ToListAsync();
        }


        public async Task<Guid> InsertAsync(Call call, CancellationToken ct = default)
        {
            try
            {
                _dbContext.call.Add(call);
                await _dbContext.SaveChangesAsync(ct);

                return call.call_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert Call record. Payload: {@Call}", call);
                throw;
            }
        }
        public async Task<List<string>> GetDistinctCallStatusesAsync()
        {
            return await _dbContext.call
                .Select(c => c.status_of_call.ToLower())
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<CallResponse>> GetCallsAsync(int companyId, string? statusFilter)
        {
            var query =
                from c in _dbContext.call
                where c.company_id == companyId

                join cd in _dbContext.call_confirm_data
                    on c.call_id equals cd.call_id into confirmGroup
                from cd in confirmGroup.DefaultIfEmpty()

                join cs in _dbContext.call_sentiment
                    on c.call_id equals cs.call_id into sentimentGroup
                from cs in sentimentGroup.DefaultIfEmpty()

                join sb in _dbContext.call_summary_bullets
                    on c.call_id equals sb.call_id into bulletGroup

                select new
                {
                    Call = c,
                    Confirm = cd,
                    Sentiment = cs,
                    Bullets = bulletGroup
                };

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var lowerFilter = statusFilter.ToLower();
                query = query.Where(x => x.Call.status_of_call.ToLower() == lowerFilter);
            }

            return await query
                .OrderByDescending(x => x.Call.call_timestamp)
                .Select(x => new CallResponse
                {
                    CallId = x.Call.call_id,
                    BrokerNumber = x.Call.broker_number,
                    StatusOfCall = x.Call.status_of_call,
                    CallTimestamp = x.Call.call_timestamp,
                    BrokerCompany = x.Call.broker_company,
                    CallDuration = x.Call.call_duration ?? 0,

                    IsTranscriptComplete = x.Call.is_transcript_complete,
                    IsAIProcessingComplete = x.Call.is_ai_processing_complete,

                    BrokerName = x.Confirm != null ? x.Confirm.broker_name : null,
                    Sentiment = x.Sentiment != null ? x.Sentiment.sentiment : null,

                    // ✅ Only include bullets if ended
                    SummaryBullets = x.Call.status_of_call == "ended"
                        ? x.Bullets
                            .OrderBy(b => b.timestamp)
                            .Select(b => b.text)
                            .ToList()
                        : null
                })
                .ToListAsync();
        }


        public async Task<Guid?> GetCallIdByTwilioSidAsync(string callSid)
        {
            return await _dbContext.call
                .Where(c => c.twilio_call_sid == callSid)
                .Select(c => c.call_id)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAfterDialAsync(
        Guid callId,
        OutboundCallResult callResult)
        {
            var call = await _dbContext.call.FindAsync(callId);
            if (call == null) return;

            // FAILED CALL
            if (callResult.Status == "failed")
            {
                call.caller_id = null;
                call.twilio_call_sid = null;
                call.status_of_call = "failed";
                call.call_result = "none";

                await _dbContext.SaveChangesAsync();
                return;
            }
            // HEALTHY CALL — IDs ARE REQUIRED
            if (string.IsNullOrWhiteSpace(callResult.UltravoxCallId))
                throw new InvalidOperationException("UltravoxCallId is required for a non-failed call.");

            if (string.IsNullOrWhiteSpace(callResult.TwilioCallSid))
                throw new InvalidOperationException("TwilioCallSid is required for a non-failed call.");

            call.caller_id = callResult.UltravoxCallId;
            call.twilio_call_sid = callResult.TwilioCallSid;

            // Status must come from callResult (ringing/live/etc.)
            call.status_of_call = "ringing";
            call.call_result = "none";

            await _dbContext.SaveChangesAsync();
        }

    }
}
