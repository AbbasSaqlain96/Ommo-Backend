using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using System;

namespace OmmoBackend.Repositories.Implementations
{
    public class AccidentDetailsRepository : GenericRepository<Accident>, IAccidentDetailsRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AccidentDetailsRepository> _logger;

        public AccidentDetailsRepository(AppDbContext dbContext, ILogger<AccidentDetailsRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> ValidateEventAndDriver(int eventId, int driverId)
        {
            try
            {
                _logger.LogInformation("Validating event {EventId} and driver {DriverId}.", eventId, driverId);
                bool exists = await _dbContext.event_driver.AnyAsync(e => e.event_id == eventId && e.driver_id == driverId);
                _logger.LogInformation("Validation result for event {EventId} and driver {DriverId}: {Result}", eventId, driverId, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while validating event {EventId} and driver {DriverId}.", eventId, driverId);
                throw;
            }
        }

        public async Task<AccidentDetailsResponse> GetAccidentDetailsAsync(int eventId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching accident details for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);

                var accidentDetailDto = await (from pe in _dbContext.performance_event
                                   join d in _dbContext.driver on pe.driver_id equals d.driver_id
                                   join a in _dbContext.accident on eventId equals a.event_id
                                   where pe.event_id == eventId && d.company_id == companyId
                                   select new AccidentDetailDto
                                   {
                                       TruckId = pe.truck_id,
                                       DriverId = pe.driver_id,
                                       DriverName = d.driver_name,
                                       TrailerId = pe.trailer_id,
                                       Location = pe.location,
                                       EventType = pe.event_type.ToString(),
                                       Authority = pe.authority.ToString(),
                                       EventDate = pe.date,
                                       Description = pe.description,
                                       LoadId = (int)pe.load_id,
                                       EventFee = pe.event_fees,
                                       FeesPaidBy = pe.fees_paid_by.ToString(),
                                       CompanyFeeApplied = pe.company_fee_applied,
                                       CompanyFeeAmount = pe.company_fee_amount,
                                       CompanyFeeStatementDate = pe.company_fee_statement_date,
                                       DriverFault = a.driver_fault,
                                       AlcoholTest = a.alcohol_test,
                                       DrugTestDateTime = a.drug_test_date_time,
                                       AlcoholTestDateTime = a.alcohol_test_date_time,
                                       HasCasualties = a.has_casuality ? true : false,
                                       DriverDrugTested = a.driver_drug_test ? true : false,
                                       TicketId = a.ticket_id,
                                       PoliceDocNumber = string.Empty,
                                       PoliceReport = string.Empty,
                                       DriverDocNumber = string.Empty,
                                       DriverReport = string.Empty,
                                       AccidentPictures = new List<string>()
                                   }).FirstOrDefaultAsync();

                if (accidentDetailDto == null)
                {
                    _logger.LogWarning("No accident details found for EventId: {EventId} and CompanyId: {CompanyId}.", eventId, companyId);
                    return null;
                }

                // Get Accident ID
                var accident = await _dbContext.accident.FirstOrDefaultAsync(a => a.event_id == eventId);
                if (accident != null)
                {
                    // Fetch associated documents
                    var accidentDocs = await _dbContext.accident_doc
                        .Where(ad => ad.accident_id == accident.accident_id)
                        .ToListAsync();

                    foreach (var doc in accidentDocs)
                    {
                        switch (doc.doc_type_id)
                        {
                            case 24: // Driver
                                accidentDetailDto.DriverDocNumber = doc.doc_number;
                                accidentDetailDto.DriverReport = doc.file_path;
                                break;
                            case 25: // Police
                                accidentDetailDto.PoliceDocNumber = doc.doc_number;
                                accidentDetailDto.PoliceReport = doc.file_path;
                                break;
                        }
                    }

                    _logger.LogInformation("Fetched {Count} accident documents for AccidentId: {AccidentId}.", accidentDocs.Count, accident.accident_id);

                    // Fetch associated pictures
                    accidentDetailDto.AccidentPictures = await _dbContext.accident_pictures
                        .Where(p => p.accident_id == accident.accident_id)
                        .Select(p => p.picture_url)
                        .ToListAsync();

                    _logger.LogInformation("Fetched {Count} accident pictures for AccidentId: {AccidentId}.", accidentDetailDto.AccidentPictures.Count, accident.accident_id);
                }


                // Fetch claims
                var claimDtos = await _dbContext.claim
                    .Where(c => c.event_id == eventId)
                    .Select(c => new ClaimDto
                    {
                        ClaimId = c.claim_id,
                        ClaimType = c.claim_type.ToString(),
                        ClaimStatus = c.status.ToString(),
                        ClaimAmount = c.claim_amount,
                        ClaimCreatedAt = c.created_at,
                        ClaimDescription = c.claim_description
                    }).ToListAsync();

                _logger.LogInformation("Successfully retrieved accident detail and {ClaimCount} claims for EventId: {EventId}.", claimDtos.Count, eventId);

                return new AccidentDetailsResponse
                {
                    accidentDetailDto = accidentDetailDto,
                    accidentClaimDtos = claimDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving accident details for EventId: {EventId}, CompanyId: {CompanyId}.", eventId, companyId);
                throw;
            }
        }

        public async Task<List<ClaimDto>> GetClaimsAsync(int eventId)
        {
            try
            {
                _logger.LogInformation("Fetching claims for EventId: {EventId}.", eventId);

                var claims = await (from c in _dbContext.claim
                                    join a in _dbContext.accident on c.event_id equals a.event_id
                                    where a.event_id == eventId
                                    select new ClaimDto
                                    {
                                        ClaimId = c.claim_id,
                                        ClaimType = c.claim_type.ToString(),
                                        ClaimStatus = c.status.ToString(),
                                        ClaimAmount = c.claim_amount,
                                        ClaimCreatedAt = c.created_at
                                    }).ToListAsync();

                _logger.LogInformation("Retrieved {Count} claims for EventId: {EventId}.", claims.Count, eventId);
                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching claims for EventId: {EventId}.", eventId);
                throw;
            }
        }

        public async Task<bool> IsEventValidForCompany(int eventId, int companyId)
        {
            try
            {
                _logger.LogInformation("Validating if EventId: {EventId} belongs to CompanyId: {CompanyId}.", eventId, companyId);

                bool isValid = await (
                from pe in _dbContext.performance_event
                    //join d in _dbContext.driver on pe.driver_id equals d.driver_id
                where pe.event_id == eventId && pe.company_id == companyId
                select pe
            ).AnyAsync();
                _logger.LogInformation("Validation result for EventId: {EventId}, CompanyId: {CompanyId}: {Result}.", eventId, companyId, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while validating EventId: {EventId} for CompanyId: {CompanyId}.", eventId, companyId);
                throw;
            }
        }
    }
}
