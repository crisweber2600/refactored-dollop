# EF Core Replication Guide

This guide walks through applying the EF Core conventions from `refactored-dollop` into another project.

The old `IsDeleted` flag has been replaced by the `Validated` property for indicating active entities.

## 1. Define Domain Entities

Create your entity classes implementing the common interfaces:

```csharp
public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public interface IRootEntity {}
```

```csharp
public class YourEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool Validated { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 2. Configure DbContext

Implement a `DbContext` that applies configurations from the assembly and adds a global soft delete filter.

```csharp
public class YourDbContext : DbContext
{
    public DbSet<YourEntity> YourEntities => Set<YourEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourDbContext).Assembly);

        foreach (var type in modelBuilder.Model.GetEntityTypes()
                 .Select(e => e.ClrType)
                 .Where(t => typeof(IValidatable).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var param = Expression.Parameter(type, "e");
            var body = Expression.Equal(
                Expression.Property(param, nameof(IValidatable.Validated)),
                Expression.Constant(true)
            );
            var lambda = Expression.Lambda(body, param);
            modelBuilder.Entity(type).HasQueryFilter(lambda);
        }
    }
}
```

## 3. Fluent Entity Configuration

Place `IEntityTypeConfiguration<T>` implementations in the same assembly so they are discovered automatically by `ApplyConfigurationsFromAssembly`.

```csharp
public class YourEntityConfiguration : IEntityTypeConfiguration<YourEntity>
{
    public void Configure(EntityTypeBuilder<YourEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
    }
}
```

## 4. Generic Repository

Define a generic repository interface and an EF Core implementation respecting the soft delete pattern.

```csharp
public interface IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    Task<T?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task DeleteAsync(T entity, bool hardDelete = false);
    Task<int> CountAsync();
}
```

```csharp
public class EfGenericRepository<T> : IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly YourDbContext _context;
    private readonly DbSet<T> _set;

    public EfGenericRepository(YourDbContext context)
    {
        _context = context;
        _set = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = includeDeleted ? _set.IgnoreQueryFilters() : _set;
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<List<T>> GetAllAsync() => _set.ToListAsync();

    public Task AddAsync(T entity) => _set.AddAsync(entity).AsTask();

    public async Task DeleteAsync(T entity, bool hardDelete = false)
    {
        if (hardDelete)
            _set.Remove(entity);
        else
        {
            entity.Validated = false;
            _set.Update(entity);
        }
        await _context.SaveChangesAsync();
    }

    public Task<int> CountAsync() => _set.CountAsync();
}
```

## 5. Unit of Work

Create a unit-of-work abstraction to access repositories and commit changes.

```csharp
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
}
```

```csharp
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;

    public UnitOfWork(TContext context) => _context = context;

    public IRepository<T> Repository<T>() where T : class => new EfRepository<T>(_context);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
```

## 6. Dependency Injection Setup

Register the DbContext, repositories and unit of work in `Program.cs` or the startup configuration.

```csharp
services.AddDbContext<YourDbContext>(opts =>
    opts.UseSqlite(Configuration.GetConnectionString("Default")));

services.AddScoped<IValidationService, ValidationService>();
services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
```

This replicates the conventions used in `refactored-dollop` and provides a clean template for integrating EF Core with soft deletes, repositories and a unit-of-work pattern.
