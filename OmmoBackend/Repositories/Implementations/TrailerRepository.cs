using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class TrailerRepository : GenericRepository<Trailer>, ITrailerRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TrailerRepository> _logger;
        public TrailerRepository(AppDbContext dbContext, ILogger<TrailerRepository> logger) : base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Trailer?> GetTrailerInfoByUnitIdAsync(int unitId)
        {
            _logger.LogInformation("Fetching trailer information for UnitId: {UnitId}", unitId);

            try
            {
                // Fetch Trailer object based on the Unit's Trailer Id
                var trailer = await (from u in _dbContext.unit
                                     join t in _dbContext.trailer on u.trailer_id equals t.trailer_id
                                     where u.unit_id == unitId
                                     select t).FirstOrDefaultAsync();

                if (trailer != null)
                {
                    _logger.LogInformation("Trailer found for UnitId: {UnitId} with TrailerId: {TrailerId}", unitId, trailer.trailer_id);
                }
                else
                {
                    _logger.LogWarning("No trailer found for UnitId: {UnitId}", unitId);
                }

                return trailer;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database operation failed while fetching trailer for UnitId: {UnitId}", unitId);
                throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching trailer for UnitId: {UnitId}", unitId);
                throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
            }
        }

        // public async Task<TruckTrailerLocation?> GetTrailerLocationByTrailerId(int trailerId)
        // {
        //     try
        //     {
        //         // Fetch Trailer Location from the Truck_Trailer_Location table
        //         var trailerLocation = await _dbContext.truck_trailer_location
        //             .Where(tl => tl.vehicle_type == "Trailer" && tl.vehicle_id == trailerId)
        //             .OrderByDescending(tl => tl.last_updated_location)
        //             .FirstOrDefaultAsync();

        //         return trailerLocation;
        //     }
        //     catch (DbUpdateException dbEx) 
        //     {
        //         throw new DataAccessException(ErrorMessages.DatabaseOperationFailed, dbEx);
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new DataAccessException(ErrorMessages.GenericOperationFailed, ex);
        //     }
        // }

        public async Task<bool> IsValidTrailerIdAsync(int trailerId, int companyId)
        {
            return await _dbContext.trailer.AnyAsync(t => t.trailer_id == trailerId);
        }


        public async Task<bool> DoesTrailerBelongToCompanyAsync(int trailerId, int companyId)
        {
            return await _dbContext.trailer
                .Where(t => t.trailer_id == trailerId)
                .Join(_dbContext.vehicle,
                      t => t.vehicle_id,
                      v => v.vehicle_id,
                      (t, v) => new { t, v })
                .Join(_dbContext.carrier,
                      tv => tv.v.carrier_id,
                      c => c.carrier_id,
                      (tv, c) => new { tv.t, c })
                .AnyAsync(result => result.c.company_id == companyId);
        }

        public async Task<bool> ExistsAsync(int trailerId)
        {
            return await _dbContext.trailer
                .AnyAsync(tr => tr.trailer_id == trailerId);
        }
    }
}