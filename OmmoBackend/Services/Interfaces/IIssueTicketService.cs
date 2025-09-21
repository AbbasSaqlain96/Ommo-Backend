using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IIssueTicketService
    {
        //Task<ServiceResponse<IssueTicketResult>> CreateIssueTicketAsync(IssueTicketRequest request);
        //Task<ServiceResponse<IEnumerable<IssueTicketDto>>> GetIssueTicketsByCompanyIdAsync(int companyId);

        Task<ServiceResponse<List<IssueTicketResponseDto>>> GetIssueTicketsAsync(int companyId);
        Task<ServiceResponse<IssueTicketResult>> CreateIssueTicketAsync(CreateIssueTicketRequest request, int companyId, int userId);

        Task<ServiceResponse<IssueTicketResult>> UpdateIssueTicketAsync(UpdateIssueTicketRequest request, int userId, int companyId);
    }
}