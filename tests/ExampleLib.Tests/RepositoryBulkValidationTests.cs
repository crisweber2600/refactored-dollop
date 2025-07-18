using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Integration tests for repository bulk validation functionality.
/// Tests how the bulk validation integrates with basic repository patterns.
/// </summary>
public class RepositoryBulkValidationTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    /// <summary>
    /// Test repository implementation that demonstrates bulk validation integration.
    /// </summary>
    public class TestRepository
    {
        private readonly TestDbContext _context;
        private readonly IValidationRunner _validationRunner;

        public TestRepository(TestDbContext context, IValidationRunner validationRunner)
        {
            _context = context;
            _validationRunner = validationRunner;
        }

        public async Task AddManyAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return;

            // Use ValidationRunner for bulk validation
            var isValid = await _validationRunner.ValidateManyAsync(entityList, cancellationToken);
            
            // Set validation status for all entities
            foreach (var entity in entityList)
            {
                entity.Validated = isValid;
            }
            
            _context.TestEntities.AddRange(entityList);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateManyAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return;

            // Use ValidationRunner for bulk validation
            var isValid = await _validationRunner.ValidateManyAsync(entityList, cancellationToken);
            
            // Set validation status for all entities
            foreach (var entity in entityList)
            {
                entity.Validated = isValid;
            }
            
            _context.TestEntities.UpdateRange(entityList);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ValidateManyAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default)
        {
            return await _validationRunner.ValidateManyAsync(entities, cancellationToken);
        }
    }

    #region Test Repository Bulk Validation Tests

    [Fact]
    public async Task TestRepository_AddManyAsync_WithValidEntities_CallsValidationRunner()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-add-many-valid")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "Test2", Value = 200 },
            new TestEntity { Id = 3, Name = "Test3", Value = 300 }
        };

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        mockValidationRunner.Verify(v => v.ValidateManyAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify entities were added to context
        Assert.Equal(3, context.TestEntities.Count());
        
        // Verify all entities have Validated = true
        foreach (var entity in entities)
        {
            Assert.True(entity.Validated);
        }
    }

    [Fact]
    public async Task TestRepository_AddManyAsync_WithInvalidEntities_SetsValidatedToFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-add-many-invalid")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(false); // Validation fails

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "Test2", Value = 200 }
        };

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        mockValidationRunner.Verify(v => v.ValidateManyAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify entities were added to context
        Assert.Equal(2, context.TestEntities.Count());
        
        // Verify all entities have Validated = false
        foreach (var entity in entities)
        {
            Assert.False(entity.Validated);
        }
    }

    [Fact]
    public async Task TestRepository_AddManyAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-add-many-null")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddManyAsync(null!));
    }

    [Fact]
    public async Task TestRepository_AddManyAsync_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-add-many-empty")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>();

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        mockValidationRunner.Verify(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, context.TestEntities.Count());
    }

    [Fact]
    public async Task TestRepository_UpdateManyAsync_WithValidEntities_CallsValidationRunner()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-update-many-valid")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "Test2", Value = 200 }
        };

        // First add entities to context so they can be updated
        context.TestEntities.AddRange(entities);
        await context.SaveChangesAsync();

        // Modify entities
        entities[0].Name = "Updated Test1";
        entities[1].Name = "Updated Test2";

        // Act
        await repository.UpdateManyAsync(entities);

        // Assert
        mockValidationRunner.Verify(v => v.ValidateManyAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify all entities have Validated = true
        foreach (var entity in entities)
        {
            Assert.True(entity.Validated);
        }
    }

    [Fact]
    public async Task TestRepository_ValidateManyAsync_CallsValidationRunner()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("test-repo-validate-many")
            .Options;

        var mockValidationRunner = new Mock<IValidationRunner>();
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "Test2", Value = 200 }
        };

        var cancellationToken = new CancellationToken();

        // Act
        var result = await repository.ValidateManyAsync(entities, cancellationToken);

        // Assert
        Assert.True(result);
        mockValidationRunner.Verify(v => v.ValidateManyAsync(entities, cancellationToken), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task BulkValidation_IntegrationTest_WithRealValidationRunner()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("bulk-validation-integration-test"));
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("bulk-validation-integration-test-audit"));

        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("BulkValidationTest")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestEntity>(entity => entity.Value, ThresholdType.RawDifference, 100.0m)
                  .AddValidationRules<TestEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value > 0);
        });

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<TestDbContext>();
        var validationRunner = provider.GetRequiredService<IValidationRunner>();

        var repository = new TestRepository(context, validationRunner);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "Test2", Value = 200 },
            new TestEntity { Id = 3, Name = "Test3", Value = 300 }
        };

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        Assert.Equal(3, context.TestEntities.Count());
        
        // Verify all entities were validated successfully
        foreach (var entity in entities)
        {
            Assert.True(entity.Validated);
        }
    }

    [Fact]
    public async Task BulkValidation_IntegrationTest_WithFailingValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("bulk-validation-integration-test-fail"));
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("bulk-validation-integration-test-audit-fail"));

        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("BulkValidationTestFail")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestEntity>(entity => entity.Value, ThresholdType.RawDifference, 100.0m)
                  .AddValidationRules<TestEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name), // This will fail for empty names
                      entity => entity.Value > 0);
        });

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<TestDbContext>();
        var validationRunner = provider.GetRequiredService<IValidationRunner>();

        var repository = new TestRepository(context, validationRunner);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100 },
            new TestEntity { Id = 2, Name = "", Value = 200 }, // This will fail validation
            new TestEntity { Id = 3, Name = "Test3", Value = 300 }
        };

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        Assert.Equal(3, context.TestEntities.Count());
        
        // Verify all entities were marked as invalid due to bulk validation failure
        foreach (var entity in entities)
        {
            Assert.False(entity.Validated);
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task BulkValidation_WithLargeCollection_PerformsWell()
    {
        // Arrange
        var mockValidationRunner = new Mock<IValidationRunner>();
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("bulk-validation-performance-test")
            .Options;

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        // Create a large collection
        var entities = new List<TestEntity>();
        for (int i = 1; i <= 10000; i++)
        {
            entities.Add(new TestEntity { Id = i, Name = $"Test{i}", Value = i * 10 });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await repository.AddManyAsync(entities);

        // Assert
        stopwatch.Stop();
        
        // Verify the operation completed in reasonable time (should be much faster than individual validation)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Bulk validation took {stopwatch.ElapsedMilliseconds}ms, which is too slow");
        
        // Verify ValidationRunner was called only once for the entire collection
        mockValidationRunner.Verify(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify all entities were processed
        Assert.Equal(10000, context.TestEntities.Count());
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public async Task BulkValidation_ComparedToIndividualValidation_ShowsPerformanceGain()
    {
        // Arrange
        var mockValidationRunner = new Mock<IValidationRunner>();
        
        // Setup individual validation calls
        mockValidationRunner.Setup(v => v.ValidateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);
        
        // Setup bulk validation call
        mockValidationRunner.Setup(v => v.ValidateManyAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("bulk-validation-comparison-test")
            .Options;

        using var context = new TestDbContext(options);
        var repository = new TestRepository(context, mockValidationRunner.Object);

        var entities = new List<TestEntity>();
        for (int i = 1; i <= 100; i++)
        {
            entities.Add(new TestEntity { Id = i, Name = $"Test{i}", Value = i * 10 });
        }

        // Act - Bulk validation
        await repository.ValidateManyAsync(entities);

        // Assert - Bulk validation should be called once for all entities
        mockValidationRunner.Verify(v => v.ValidateManyAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
        
        // Individual validation would have been called 100 times
        mockValidationRunner.Verify(v => v.ValidateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}