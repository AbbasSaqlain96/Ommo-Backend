using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class ViolationRepository : GenericRepository<Violation>, IViolationRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ViolationRepository> _logger;
        public ViolationRepository(AppDbContext dbContext, ILogger<ViolationRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateViolationsAsync(int ticketId, List<int> violations)
        {
            _logger.LogInformation("Creating violations for TicketId: {TicketId}, Violations Count: {Count}", ticketId, violations.Count);

            try
            {
                var violationTickets = violations.Select(v => new ViolationTicket
                {
                    ticket_id = ticketId,
                    violation_id = v,
                    violation_date = DateTime.UtcNow
                }).ToList();

                _dbContext.ticket_violation.AddRange(violationTickets);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created {Count} violations for TicketId: {TicketId}", violations.Count, ticketId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating violations for TicketId: {TicketId}", ticketId);
                throw new DataAccessException("An error occurred while saving violations.", ex);
            }
        }

        // Fetch violations by a list of violation IDs
        public async Task<List<Violation>> GetByIdsAsync(List<int> violationIds)
        {
            if (violationIds == null || !violationIds.Any())
            {
                return new List<Violation>();  // Return an empty list if no IDs are provided
            }

            return await _dbContext.violation
                .Where(v => violationIds.Contains(v.violation_id))
                .ToListAsync();
        }

        //public async Task UpdateViolationsAsync(int ticketId, List<int> newViolationIds)
        //{
        //    var ticket = await _dbContext.ticket_violation.FirstOrDefaultAsync(t => t.ticket_id == ticketId);
        //    if (ticket == null)
        //    {
        //        _logger.LogWarning("No ticket found for Event ID: {EventId}", ticketId);
        //        throw new ArgumentException("Invalid ticket ID provided.");
        //    }

        //    // Validate all violation IDs exist
        //    var existingViolationsInDb = await _dbContext.violation
        //        .Where(v => newViolationIds.Contains(v.violation_id))
        //        .Select(v => v.violation_id)
        //        .ToListAsync();

        //    var invalidIds = newViolationIds.Except(existingViolationsInDb).ToList();
        //    if (invalidIds.Any())
        //    {
        //        var msg = $"The following violation IDs do not exist: {string.Join(", ", invalidIds)}";
        //        _logger.LogWarning(msg);
        //        throw new ArgumentException(msg);
        //    }

        //    // Fetch existing violation IDs from the DB
        //    var existingViolations = await _dbContext.ticket_violation
        //        .Where(tv => tv.ticket_id == ticketId)
        //        .Select(tv => tv.violation_id)
        //        .ToListAsync();

        //    var newViolationSet = new HashSet<int>(newViolationIds);

        //    // Find violations to remove (in DB but not in input)
        //    var toRemove = existingViolations
        //        .Where(v => !newViolationSet.Contains(v))
        //        .ToList();

        //    // Find violations to add (in input but not in DB)
        //    var toAdd = newViolationIds
        //        .Where(v => !existingViolations.Contains(v))
        //        .Distinct()
        //        .ToList();

        //    // Remove old violations
        //    if (toRemove.Any())
        //    {
        //        var removeEntities = await _dbContext.ticket_violation
        //            .Where(tv => tv.ticket_id == ticketId && toRemove.Contains(tv.violation_id))
        //            .ToListAsync();

        //        _dbContext.ticket_violation.RemoveRange(removeEntities);
        //    }

        //    // Add new violations
        //    foreach (var violationId in toAdd)
        //    {
        //        _dbContext.ticket_violation.Add(new ViolationTicket
        //        {
        //            ticket_id = ticketId,
        //            violation_id = violationId
        //        });
        //    }

        //    await _dbContext.SaveChangesAsync();
        //}

        public async Task UpdateTicketViolationsAsync(int ticketId, List<int?>? newViolationIds)
        {
            var ticket = await _dbContext.ticket_violation
                .FirstOrDefaultAsync(t => t.ticket_id == ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("No ticket found for Event ID: {EventId}", ticketId);
                throw new ArgumentException($"No Ticket found for ticket_id = {ticketId}");
            }

            // Filter non-null and unique IDs
            var requestedIds = newViolationIds
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            if (!requestedIds.Any())
                throw new ArgumentException("No valid violation IDs provided.");

            // Get only valid violation IDs from the database
            var validViolationIds = await _dbContext.violation
                .Where(v => requestedIds.Contains(v.violation_id))
                .Select(v => v.violation_id)
                .ToListAsync();

            var invalidIds = requestedIds.Except(validViolationIds).ToList();

            if (!validViolationIds.Any())
            {
                _logger.LogWarning("All provided violation IDs are invalid. Existing violations were NOT deleted.");
                throw new ArgumentException("All provided violation IDs are invalid. Update aborted.");
            }

            if (invalidIds.Any())
            {
                _logger.LogWarning("Some violation IDs are invalid and were skipped: {InvalidIds}", string.Join(", ", invalidIds));
            }

            // Delete old violations
            var oldViolations = await _dbContext.ticket_violation
                .Where(v => v.ticket_id == ticketId)
                .ToListAsync();

            _dbContext.ticket_violation.RemoveRange(oldViolations);

            // Add valid new violations
            foreach (var violationId in validViolationIds)
            {
                _dbContext.ticket_violation.Add(new ViolationTicket
                {
                    ticket_id = ticketId,
                    violation_id = violationId,
                    violation_date = DateTime.Now
                });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateDotInspectionViolationsAsync(int eventId, List<int?>? newViolationIds)
        {
            // Get inspection ID for document
            var docInspection = await _dbContext.doc_inspection
                .FirstOrDefaultAsync(x => x.event_id == eventId);

            if (docInspection == null)
                throw new ArgumentException($"No DocInspection found for event_id = {eventId}");

            int docInspectionId = docInspection.doc_inspection_id;

            // Filter non-null and unique IDs
            var requestedIds = newViolationIds
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            if (!requestedIds.Any())
                throw new ArgumentException("No valid violation IDs provided.");

            // Get only valid violation IDs from the database
            var validViolationIds = await _dbContext.violation
                .Where(v => requestedIds.Contains(v.violation_id))
                .Select(v => v.violation_id)
                .ToListAsync();

            var invalidIds = requestedIds.Except(validViolationIds).ToList();

            if (!validViolationIds.Any())
            {
                _logger.LogWarning("All provided violation IDs are invalid. Existing violations were NOT deleted.");
                throw new ArgumentException("All provided violation IDs are invalid. Update aborted.");
            }

            if (invalidIds.Any())
            {
                _logger.LogWarning("Some violation IDs are invalid and were skipped: {InvalidIds}", string.Join(", ", invalidIds));
            }

            // Delete old violations
            var oldViolations = await _dbContext.doc_inspection_violation
                .Where(v => v.doc_inspection_id == docInspectionId)
                .ToListAsync();

            _dbContext.doc_inspection_violation.RemoveRange(oldViolations);

            // Add valid new violations
            foreach (var violationId in validViolationIds)
            {
                _dbContext.doc_inspection_violation.Add(new DocInspectionViolation
                {
                    doc_inspection_id = docInspectionId,
                    violation_id = violationId,
                    violation_date = DateTime.Now
                });
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateWarningViolationsAsync(int eventId, List<int?>? newViolationIds)
        {
            // Get warning ID for document
            var warning = await _dbContext.warning
                .FirstOrDefaultAsync(x => x.event_id == eventId);

            if (warning == null)
                throw new ArgumentException($"No Warning found for event_id = {eventId}");

            int warningId = warning.warning_id;

            // Filter non-null and unique IDs
            var requestedIds = newViolationIds
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            if (!requestedIds.Any())
                throw new ArgumentException("No valid violation IDs provided.");

            // Get only valid violation IDs from the database
            var validViolationIds = await _dbContext.violation
                .Where(v => requestedIds.Contains(v.violation_id))
                .Select(v => v.violation_id)
                .ToListAsync();

            var invalidIds = requestedIds.Except(validViolationIds).ToList();

            if (!validViolationIds.Any())
            {
                _logger.LogWarning("All provided violation IDs are invalid. Existing violations were NOT deleted.");
                throw new ArgumentException("All provided violation IDs are invalid. Update aborted.");
            }

            if (invalidIds.Any())
            {
                _logger.LogWarning("Some violation IDs are invalid and were skipped: {InvalidIds}", string.Join(", ", invalidIds));
            }

            // Delete old ones
            var oldViolations = await _dbContext.warning_violation
                .Where(v => v.warning_id == warningId)
                .ToListAsync();

            _dbContext.warning_violation.RemoveRange(oldViolations);

            // Add valid new violations
            foreach (var violationId in validViolationIds)
            {
                _dbContext.warning_violation.Add(new WarningViolation
                {
                    warning_id = warningId,
                    violation_id = violationId,
                    violation_date = DateTime.Now
                });
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
