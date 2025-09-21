using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/trailer")]
    public class TrailerController : ControllerBase
    {
        private readonly ITrailerService _trailerService;
        private readonly ILogger<TrailerController> _logger;

        /// <summary>
        /// Initializes a new instance of the TrailerController class with the specified trailer service.
        /// </summary>
        public TrailerController(ITrailerService trailerService, ILogger<TrailerController> logger)
        {
            _trailerService = trailerService;
            _logger = logger;
        }

        [HttpGet("get-trailer-info")]
        [Authorize]
        [RequireAuthenticationOnly]
        public async Task<IActionResult> GetTrailerInfo(int unitId)
        {
            _logger.LogInformation("Received request to get trailer info for Unit ID: {UnitId}", unitId);

            if (unitId <= 0)
            {
                _logger.LogWarning("Invalid Unit ID provided: {UnitId}", unitId);
                return BadRequest(new { errorMessage = "Invalid Unit ID provided" });
            }

            try
            {
                _logger.LogInformation("Fetching trailer info for Unit ID: {UnitId}", unitId);

                // Call the service method to get trailer info
                var trailerInfo = await _trailerService.GetTrailerInfoAsync(unitId);

                // Check if the trailer was found
                if (trailerInfo == null)
                {
                    _logger.LogWarning("No trailer information found for Unit ID: {UnitId}", unitId);
                    return NotFound(new { errorMessage = ErrorMessages.ResourceNotFound("Trailer information") });
                }

                _logger.LogInformation("Successfully retrieved trailer info for Unit ID: {UnitId}", unitId);
                // Return the trailer object if found
                return Ok(trailerInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving trailer info for Unit ID: {UnitId}", unitId);
                return StatusCode(500, new { errorMessage = ErrorMessages.InternalServerError });
            }
        }
    }
}