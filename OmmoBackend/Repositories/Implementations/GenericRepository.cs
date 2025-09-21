using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmmoBackend.Data;
using OmmoBackend.Repositories.Interfaces;

namespace OmmoBackend.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _dbContext; // Database context for accessing the database
        private readonly DbSet<T> _dbSet; // DbSet representing the collection of entities of type T
        private readonly ILogger<GenericRepository<T>> _logger;

        /// <summary>
        /// Initializes a new instance of the GenericRepository class with the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context used to access the database.</param>
        public GenericRepository(AppDbContext dbContext, ILogger<GenericRepository<T>> logger)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>(); // Initialize the DbSet for the specific entity type
            _logger = logger;
        }

        /// <summary>
        /// Adds a new entity asynchronously to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public async Task AddAsync(T entity)
        {
            try
            {
                _logger.LogInformation("Adding a new entity of type {EntityType}", typeof(T).Name);
                await _dbSet.AddAsync(entity);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Entity of type {EntityType} added successfully", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding an entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing entity in the database asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public async Task UpdateAsync(T entity)
        {
            try
            {
                _logger.LogInformation("Updating an entity of type {EntityType}", typeof(T).Name);
                _dbSet.Update(entity);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Entity of type {EntityType} updated successfully", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating an entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Deletes an existing entity from the database asynchronously.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public async Task DeleteAsync(T entity)
        {
            try
            {
                _logger.LogInformation("Deleting an entity of type {EntityType}", typeof(T).Name);
                _dbSet.Remove(entity);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Entity of type {EntityType} deleted successfully", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting an entity of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all entities of type T from the database asynchronously.
        /// </summary>
        /// <returns>A list of all entities of type T.</returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all entities of type {EntityType}", typeof(T).Name);
                var result = await _dbSet.ToListAsync();
                _logger.LogInformation("{Count} entities of type {EntityType} retrieved successfully", result.Count, typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all entities of type {EntityType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an entity by its primary key asynchronously.
        /// </summary>
        /// <param name="id">The primary key of the entity.</param>
        /// <returns>The entity with the specified primary key, or null if not found.</returns>
        public async Task<T?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving an entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Entity of type {EntityType} with ID {Id} not found", typeof(T).Name, id);
                }
                else
                {
                    _logger.LogInformation("Entity of type {EntityType} with ID {Id} retrieved successfully", typeof(T).Name, id);
                }
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving an entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
                throw;
            }
        }
    }
}