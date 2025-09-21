using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Dtos;
using OmmoBackend.Models;

namespace OmmoBackend.Repositories.Interfaces
{
    public interface IIssueTicketRepository : IGenericRepository<IssueTicket>
    {
        Task<IEnumerable<IssueTicket>> GetTicketsByCompanyIdAsync(int carrierId);
        Task<List<IssueTicket>> GetTicketsByCategoryIdAsync(int categoryId);

        Task<List<IssueTicketResponseDto>> GetIssueTicketsAsync(int companyId);

        Task<int> CreateIssueTicketAsync(IssueTicket issueTicket);

        Task<List<string>> SaveTicketFilesAsync(int ticketId, List<IFormFile> fileNames, int companyId, int? vehicleId);

        //Task<bool> ValidateCompanyEntities(int vehicleId, int userId, int categoryId, int carrierId);

        Task<int> GetCompanyIdByTicketIdAsync(int ticketId);

        //Task<bool> ValidateCompanyEntitiesToUpdateIssueTicket(int? vehicleId, int? userId, int? categoryId, int carrierId);
        Task<(bool vehicleValid, bool userValid, bool categoryValid)> ValidateCompanyEntitiesToUpdateIssueTicket(
            int? vehicleId, int? userId, int? categoryId, int carrierId);

        Task<(bool VehicleValid, bool UserValid, bool CategoryValid)> ValidateCompanyEntitiesDetailed(
            int vehicleId, int userId, int categoryId, int carrierId);
    }
}