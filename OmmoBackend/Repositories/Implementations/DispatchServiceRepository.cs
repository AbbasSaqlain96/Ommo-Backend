using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Data;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class DispatchServiceRepository : GenericRepository<DispatchService>, IDispatchServiceRepository
    {
        private readonly ILogger<DispatchServiceRepository> _logger;
        public DispatchServiceRepository(AppDbContext dbContext, ILogger<DispatchServiceRepository> logger) : base(dbContext, logger)
        {
        }
    }
}