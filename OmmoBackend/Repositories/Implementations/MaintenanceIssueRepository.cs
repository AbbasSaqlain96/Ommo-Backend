using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Dtos;
using OmmoBackend.Exceptions;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    //public class MaintenanceIssueRepository : GenericRepository<MaintenanceIssue>, IMaintenanceIssueRepository
    //{
    //    private readonly AppDbContext _dbContext;
    //    public MaintenanceIssueRepository(AppDbContext dbContext) : base(dbContext)
    //    {
    //        _dbContext = dbContext;
    //    }

    //    public async Task<IEnumerable<MaintenanceIssueDto>> GetMaintenanceIssuesAsync(string? issueType, string? issueCategory)
    //    {
    //        try
    //        {
    //            var issueTypeEnum = Enum.TryParse<IssueType>(issueType, true, out var parsedIssueType)
    //            ? parsedIssueType
    //            : throw new ArgumentException($"Invalid issue type: {issueType}");

    //            var issueCategoryEnum = Enum.TryParse<IssueCat>(issueCategory, true, out var parsedIssueCat)
    //                ? parsedIssueCat
    //                : throw new ArgumentException($"Invalid issue category: {issueCategory}");

    //            // Initial query to fetch all issues
    //            var query = _dbContext.maintenance_issue.AsQueryable();

    //            // Apply filters if provided
    //            if (!string.IsNullOrEmpty(issueType))
    //            {
    //                issueType = issueType.ToLower();
    //                query = query.Where(i => i.issue_type == issueTypeEnum);
    //            }

    //            //if (!string.IsNullOrEmpty(issueCategory))
    //            //{
    //            //    issueCategory = issueCategory.ToLower();
    //            //    query = query.Where(i => i.issue_cat == issueCategoryEnum);
    //            //}

    //            // Fetch the filtered list or all items
    //            return await query
    //                         .Select(i => new MaintenanceIssueDto
    //                         {
    //                             IssueId = i.issue_id,
    //                             IssueDescription = i.issue_description,
    //                             IssueType = i.issue_type,
    //                             //IssueCategory = i.issue_cat,
    //                             CreatedAt = i.created_at,
    //                             CompanyId = i.carrier_id,
    //                             //ScheduleInterval = i.schedule_interval
    //                         })
    //                         .ToListAsync();
    //        }
    //        catch (DbUpdateException dbEx)
    //        {
    //            throw new DataAccessException("An error occurred while accessing the maintenance issues. Please try again later.", dbEx);
    //        }
    //        catch (InvalidOperationException opEx)
    //        {
    //            throw new InvalidOperationException("Database query failed while retrieving maintenance issues.", opEx);
    //        }
    //        catch (Exception ex)
    //        {
    //            throw new ApplicationException("An unexpected error occurred while retrieving maintenance issues. Please try again later.", ex);
    //        }
    //    }
    //}
}