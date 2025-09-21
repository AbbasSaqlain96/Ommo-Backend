using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Exceptions;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class CarrierRepository : GenericRepository<Carrier>, ICarrierRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CarrierRepository> _logger;
        public CarrierRepository(AppDbContext dbContext, ILogger<CarrierRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int?> GetCarrierIdByCompanyIdAsync(int companyId)
        {
            _logger.LogInformation("Fetching Carrier ID for Company ID: {CompanyId}", companyId);

            try
            {
                // Fetch the Carrier based on Company Id
                var carrier = await _dbContext.carrier
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.company_id == companyId);

                if (carrier != null)
                {
                    _logger.LogInformation("Carrier ID {CarrierId} found for Company ID: {CompanyId}", carrier.carrier_id, companyId);
                }
                else
                {
                    _logger.LogWarning("No Carrier found for Company ID: {CompanyId}", companyId);
                }

                // Return the Carrier Id if found; otherwise, return null
                return carrier?.carrier_id;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while accessing carrier data for Company ID: {CompanyId}", companyId);
                throw new DataAccessException("An error occurred while accessing carrier data from the database.", dbEx);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Multiple carriers found for Company ID: {CompanyId}", companyId);
                throw new InvalidOperationException("Multiple carriers found for the given Company ID.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving carrier information for Company ID: {CompanyId}", companyId);
                throw new ApplicationException("An unexpected error occurred while retrieving carrier information.", ex);
            }
        }

        public async Task<Carrier?> GetCarrierByCompanyIdAsync(int companyId)
        {
            _logger.LogInformation("Fetching Carrier ID for Company ID: {CompanyId}", companyId);

            try
            {
                // Fetch the Carrier based on Company Id
                var carrier = await _dbContext.carrier
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.company_id == companyId);

                if (carrier != null)
                {
                    _logger.LogInformation("Carrier ID {CarrierId} found for Company ID: {CompanyId}", carrier.carrier_id, companyId);
                }
                else
                {
                    _logger.LogWarning("No Carrier found for Company ID: {CompanyId}", companyId);
                }

                // Return the Carrier Id if found; otherwise, return null
                return carrier;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while accessing carrier data for Company ID: {CompanyId}", companyId);
                throw new DataAccessException("An error occurred while accessing carrier data from the database.", dbEx);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Multiple carriers found for Company ID: {CompanyId}", companyId);
                throw new InvalidOperationException("Multiple carriers found for the given Company ID.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving carrier information for Company ID: {CompanyId}", companyId);
                throw new ApplicationException("An unexpected error occurred while retrieving carrier information.", ex);
            }
        }

    }
}