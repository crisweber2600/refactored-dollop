using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using WorkerService1.Models;

namespace WorkerService1.Repositories
{
    /// <summary>
    /// Entity Framework repository implementation showing how to integrate ExampleLib.Infrastructure.ValidationRunner.
    /// This demonstrates the pattern for adding validation to existing EF repositories.
    /// </summary>
    public class EfRepository<T> : IRepository<T> where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly IValidationRunner _validationRunner;

        public EfRepository(DbContext context, IValidationRunner validationRunner)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _validationRunner = validationRunner;
        }

        public async Task<T?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task<List<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task AddAsync(T entity)
        {
            // INTEGRATION POINT: Use ExampleLib ValidationRunner for comprehensive validation
            // This includes manual validation, summarisation validation, and sequence validation
            var isValid = await _validationRunner.ValidateAsync(entity);
            entity.Validated = isValid;
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            // Run validation before updating
            var isValid = await _validationRunner.ValidateAsync(entity);
            entity.Validated = isValid;
            
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateAsync(T entity, CancellationToken cancellationToken = default)
        {
            // INTEGRATION POINT: Expose ValidationRunner functionality to repository consumers
            return await _validationRunner.ValidateAsync(entity, cancellationToken);
        }

        public async Task<T?> GetLastAsync()
        {
            return await _dbSet.OrderByDescending(e => EF.Property<int>(e, "Id")).FirstOrDefaultAsync();
        }
    }

    /// <summary>
    /// Database context for WorkerService1 entities.
    /// </summary>
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }
        public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();
        public DbSet<OtherEntity> OtherEntities => Set<OtherEntity>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure SampleEntity
            modelBuilder.Entity<SampleEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Validated).IsRequired();
            });

            // Configure OtherEntity
            modelBuilder.Entity<OtherEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired();
                entity.Property(e => e.Amount).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.Validated).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}