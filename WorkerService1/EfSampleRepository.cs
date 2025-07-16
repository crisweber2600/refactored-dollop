using Microsoft.EntityFrameworkCore;
using WorkerService1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExampleLib.Domain;
using System;

namespace WorkerService1.Repositories
{
    public class EfSampleRepository : ISampleRepository<SampleEntity>
    {
        private readonly SampleDbContext _context;
        public EfSampleRepository(SampleDbContext context)
        {
            _context = context;
        }

        public async Task<SampleEntity?> GetByIdAsync(int id)
            => await _context.SampleEntities.FindAsync(id);

        public async Task<List<SampleEntity>> GetAllAsync()
            => await _context.SampleEntities.ToListAsync();

        public async Task AddAsync(SampleEntity entity)
        {
            _context.SampleEntities.Add(entity);
        }

        public async Task UpdateAsync(SampleEntity entity)
        {
            _context.SampleEntities.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.SampleEntities.FindAsync(id);
            if (entity != null)
            {
                _context.SampleEntities.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }
        public DbSet<SampleEntity> SampleEntities => Set<SampleEntity>();
    }
}