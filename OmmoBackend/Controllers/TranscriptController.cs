using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Utilities;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/Transcript")]
    public class TranscriptController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TranscriptController> _logger;
        public TranscriptController(AppDbContext db, ILogger<TranscriptController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class TranscriptRequest
        {
            public Guid CallId { get; set; }
        }

        [HttpPost("gettranscript")]
        public async Task<IActionResult> GetTranscript([FromBody] TranscriptRequest request)
        {
            if (request == null || request.CallId == Guid.Empty)
                return BadRequest("CallId is required.");

            if (!TokenHelper.TryGetCompanyId(User, _logger, out int companyId, out IActionResult? error))
                return error;

            // Fetch payment profile + package plan
            var packageData = await
                (from cpp in _db.companies_payment_profile

                 join pp in _db.package_plan
                     on cpp.subscription_plan equals pp.plan_id into planGroup
                 from pp in planGroup.DefaultIfEmpty()

                 where cpp.company_id == companyId

                 select new
                 {
                     PlanType = pp != null ? pp.plan_type : (PlanType?)null,
                     IsTranscriptAllowed = pp != null && pp.is_transcript_allowed
                 })
                .FirstOrDefaultAsync();

            if (packageData == null)
            {
                return StatusCode(403, new
                {
                    message = "Your package does not support this feature.Upgrade your plan to access it.",
                });
            }

            // Allow if:
            // 1. Plan is custom
            // OR
            // 2. Transcript feature enabled
            bool canAccessTranscript =
                packageData.PlanType == PlanType.custom
                || packageData.IsTranscriptAllowed;

            if (!canAccessTranscript)
            {
                return StatusCode(403, new
                {
                    message = "Your package does not support this feature.Upgrade your plan to access it.",
                });
            }

            var callBelongsToCompany = await _db.call
                .AnyAsync(c => c.call_id == request.CallId
                && c.company_id == companyId);

            if (!callBelongsToCompany)
            {
                return NotFound(new
                {
                    message = "Transcript not found."
                });
            }

            var transcript = await _db.call_transcript
                .Where(t => t.call_id == request.CallId)
                .OrderBy(t => t.timestamp)
                .Select(t => new { t.speaker, t.text, t.timestamp })
                .ToListAsync();

            return Ok(new { total = transcript.Count, results = transcript });
        }
    }
}
