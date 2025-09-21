using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class CarrierService : ICarrierService
    {
        private readonly ICarrierRepository _carrierRepository;
        private readonly ILogger<CarrierService> _logger;
        public CarrierService(ICarrierRepository carrierRepository, ILogger<CarrierService> logger)
        {
            _carrierRepository = carrierRepository;
            _logger = logger;
        }

        public async Task<int?> GetCarrierIdByCompanyIdAsync(int companyId)
        {
            _logger.LogInformation("Fetching CarrierId for CompanyId: {CompanyId}", companyId);

            if (companyId <= 0)
            {
                _logger.LogWarning("Invalid CompanyId: {CompanyId}. Must be greater than 0.", companyId);
                throw new ArgumentOutOfRangeException(nameof(companyId), "Company Id must be greater than 0.");
            }

            try
            {
                var carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);
                if (carrierId == null)
                {
                    _logger.LogWarning("No CarrierId found for CompanyId: {CompanyId}", companyId);
                }
                else
                {
                    _logger.LogInformation("CarrierId {CarrierId} found for CompanyId: {CompanyId}", carrierId, companyId);
                }

                return carrierId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching CarrierId for CompanyId: {CompanyId}", companyId);
                throw;
            }
        }
    }
}