using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class RequestModuleRepository : GenericRepository<RequestModule>, IRequestModuleRepository
    {
        private readonly ILogger<RequestModuleRepository> _logger;
        public RequestModuleRepository(AppDbContext dbContext, ILogger<RequestModuleRepository> logger) : base(dbContext, logger)
        {
            _logger = logger;
        }
    }
}