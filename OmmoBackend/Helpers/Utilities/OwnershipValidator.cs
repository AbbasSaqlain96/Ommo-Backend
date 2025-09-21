using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Helpers.Utilities
{
    public static class OwnershipValidator
    {
        public static async Task<(bool isValid, string? error, object? entity)> ValidateTruckOwnershipAsync(
        ITruckRepository truckRepo, int truckId, int companyId)
        {
            var truck = await truckRepo.GetByIdAsync(truckId);
            if (truck == null)
                return (false, "No truck found for the provided Truck ID.", null);

            if (!await truckRepo.DoesTruckBelongToCompanyAsync(truckId, companyId))
                return (false, "Truck does not belong to your company.", null);

            return (true, null, truck);
        }


        public static async Task<(bool isValid, string? error, object? entity)> ValidateDriverOwnershipAsync(
            IDriverRepository driverRepo, int driverId, int companyId)
        {
            var driver = await driverRepo.GetByIdAsync(driverId);
            if (driver == null)
                return (false, "No driver found for the provided Driver ID.", null);

            if (driver.company_id != companyId)
                return (false, "Driver does not belong to your company.", null);

            return (true, null, driver);
        }

        public static async Task<(bool isValid, string? error, object? entity)> ValidateTrailerOwnershipAsync(
        ITrailerRepository trailerRepo, int trailerId, int companyId)
        {
            var trailer = await trailerRepo.GetByIdAsync(trailerId);
            if (trailer == null)
                return (false, "No trailer found for the provided Trailer ID.", null);

            if (!await trailerRepo.DoesTrailerBelongToCompanyAsync(trailerId, companyId))
                return (false, "Trailer does not belong to your company.", null);

            return (true, null, trailer);
        }

        public static async Task<(bool isValid, string? error, object? entity)> ValidateTicketOwnershipAsync(
        ITicketRepository ticketRepo, int ticketId, int companyId)
        {
            var ticket = await ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null)
                return (false, "No ticket found for the provided Ticket ID.", null);

            if (!await ticketRepo.DoesTicketBelongToCompanyAsync(ticketId, companyId))
                return (false, "Ticket does not belong to your company.", null);

            return (true, null, ticket);
        }
    }
}
