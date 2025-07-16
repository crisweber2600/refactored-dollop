using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;
using WorkerService1.Models;

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
    }

    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }
        public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();
        public DbSet<OtherEntity> OtherEntities => Set<OtherEntity>();
    }
}