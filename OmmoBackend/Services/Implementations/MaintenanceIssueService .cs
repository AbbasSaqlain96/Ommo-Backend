using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    //public class MaintenanceIssueService : IMaintenanceIssueService
    //{
    //    private readonly IMaintenanceIssueRepository _maintenanceIssueRepository;

    //    public MaintenanceIssueService(IMaintenanceIssueRepository maintenanceIssueRepository)
    //    {
    //        _maintenanceIssueRepository = maintenanceIssueRepository;
    //    }

    //    public async Task<ServiceResponse<CreateMaintenanceIssueResult>> CreateCustomMaintenanceIssueAsync(CreateMaintenanceIssueRequest request)
    //    {
    //        // Validate issue type
    //        if (!Enum.TryParse<IssueType>(request.IssueType, true, out var issueType) || (issueType != IssueType.recurring && issueType != IssueType.one_time))
    //            return ServiceResponse<CreateMaintenanceIssueResult>.ErrorResponse("Invalid issue type");

    //        // Validate schedule_interval based on issue type
    //        var scheduleValidationMessage = issueType switch
    //        {
    //            IssueType.one_time when request.ScheduleInterval != null => "Schedule interval should be null for One-time issues",
    //            IssueType.recurring when request.ScheduleInterval == null => "Schedule interval is required for Recurring issues",
    //            _ => null // Valid case
    //        };

    //        if (scheduleValidationMessage != null)
    //            return ServiceResponse<CreateMaintenanceIssueResult>.ErrorResponse(scheduleValidationMessage);

    //        // Issue category should always be 'customized' for this API
    //        var issue = new MaintenanceIssue
    //        {
    //            issue_description = request.Description,
    //            issue_type = issueType,
    //            //issue_cat = IssueCat.customized,
    //            carrier_id = request.CompanyId,
    //            //schedule_interval = request.IssueType == "recurring" ? request.ScheduleInterval : null,
    //            created_at = DateTime.Now
    //        };

    //        try
    //        {
    //            await _maintenanceIssueRepository.AddAsync(issue);
    //            return ServiceResponse<CreateMaintenanceIssueResult>.SuccessResponse(new CreateMaintenanceIssueResult
    //            {
    //                Success = true
    //            });
    //        }
    //        catch (Exception ex)
    //        {
    //            return ServiceResponse<CreateMaintenanceIssueResult>.ErrorResponse("An error occurred while creating the maintenance issue. Please try again.");
    //        }
    //    }

    //    public async Task<ServiceResponse<IEnumerable<MaintenanceIssueDto>>> GetMaintenanceIssuesAsync(string? issueType, string? issueCategory)
    //    {
    //        try
    //        {
    //            // Call the repository with the provided filters
    //            var issues = await _maintenanceIssueRepository.GetMaintenanceIssuesAsync(issueType!, issueCategory!);

    //            if (issues == null || !issues.Any())
    //                return ServiceResponse<IEnumerable<MaintenanceIssueDto>>.ErrorResponse("No maintenance issues found. Please create a maintenance issue first.");

    //            return ServiceResponse<IEnumerable<MaintenanceIssueDto>>.SuccessResponse(issues);
    //        }
    //        catch (Exception ex)
    //        {
    //            return ServiceResponse<IEnumerable<MaintenanceIssueDto>>.ErrorResponse("An error occurred while retrieving maintenance issues. Please try again.");
    //        }
    //    }
    //}
}