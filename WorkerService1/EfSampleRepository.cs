using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;
using WorkerService1.Models;
using System.Linq;

namespace WorkerService1.Repositories
{
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
            // Automatically validate the entity using ValidationRunner, which creates SaveAudit records
            var isValid = await _validationRunner.ValidateAsync(entity);
            
            if (!isValid)
            {
                throw new InvalidOperationException($"Entity validation failed for {typeof(T).Name} with Id {entity.Id}");
            }
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
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
            return await _validationRunner.ValidateAsync(entity, cancellationToken);
        }

        public async Task<T?> GetLastAsync()
        {
            // Assumes Id is the primary key and is int
            return await _dbSet.OrderByDescending(e => EF.Property<int>(e, "Id")).FirstOrDefaultAsync();
        }
    }

    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }
        public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();
        public DbSet<OtherEntity> OtherEntities => Set<OtherEntity>();
    }
}