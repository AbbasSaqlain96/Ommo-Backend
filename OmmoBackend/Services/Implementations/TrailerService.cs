using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class TrailerService : ITrailerService
    {
        private readonly ITrailerRepository _trailerRepository;
        private readonly ILogger<TrailerService> _logger;
        public TrailerService(ITrailerRepository trailerRepository, ILogger<TrailerService> logger)
        {
            _trailerRepository = trailerRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<TrailerInfoDto>> GetTrailerInfoAsync(int unitId)
        {
            try
            {
                _logger.LogInformation("Fetching trailer information for Unit ID: {UnitId}", unitId);

                // Fetch trailer
                var trailer = await _trailerRepository.GetTrailerInfoByUnitIdAsync(unitId);

                if (trailer == null)
                {
                    _logger.LogWarning("No trailer information found for Unit ID: {UnitId}", unitId);
                    return ServiceResponse<TrailerInfoDto>.ErrorResponse("Trailer information not found for the given Unit ID.");
                }

                // Map Trailer to TrailerDto
                var trailerDto = MapToTrailerDto(trailer);

                // Fetch trailer location
                //var trailerLocation = await _trailerRepository.GetTrailerLocationByTrailerId(trailer.trailer_id);
                _logger.LogInformation("Fetching trailer location for Trailer ID: {TrailerId}", trailer.trailer_id);
                var trailerLocation = "";
                if (trailerLocation == null)
                {
                    _logger.LogWarning("No trailer location found for Trailer ID: {TrailerId}", trailer.trailer_id);
                    return ServiceResponse<TrailerInfoDto>.ErrorResponse("Trailer location information not found.");
                }

                // Map TrailerLocation to TrailerLocationDto
                //var trailerLocationDto = MapToTrailerLocationDto(trailerLocation);

                // Combine data into TrailerInfoDto
                var trailerInfo = new TrailerInfoDto
                {
                    Trailer = trailerDto,
                    //TrailerLocation = trailerLocationDto
                    TrailerLocation = null
                };

                _logger.LogInformation("Successfully retrieved trailer information for Unit ID: {UnitId}", unitId);
                return ServiceResponse<TrailerInfoDto>.SuccessResponse(trailerInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the trailer information for Unit ID: {UnitId}", unitId);
                return ServiceResponse<TrailerInfoDto>.ErrorResponse("An error occurred while retrieving the trailer information.");
            }
        }

        private TrailerDto MapToTrailerDto(Trailer trailer) => new TrailerDto
        {
            TrailerId = trailer.trailer_id,
            TrailerType = trailer.trailer_type,
            VehicleId = trailer.vehicle_id
        };
    }
}