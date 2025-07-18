using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Comprehensive unit tests for ValidationRunner bulk validation functionality.
/// Tests the new ValidateManyAsync method and its integration with all validation types.
/// </summary>
public class ValidationRunnerBulkValidationTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    #region ValidateManyAsync Basic Tests

    [Fact]
    public async Task ValidateManyAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.ValidateManyAsync<TestEntity>(null!));
    }

    [Fact]
    public async Task ValidateManyAsync_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>();

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Never);
    }

    [Fact]
    public async Task ValidateManyAsync_WithAllValidationsPass_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        // No ValidationPlanStore, so sequence validation should be skipped
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true },
            new TestEntity { Id = 3, Name = "Test3", Value = 300, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ValidateManyAsync_WithManualValidationFails_ReturnsFalseAndStopsEarly()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        
        // Set up manual validator to fail on second entity
        mockManualValidator.SetupSequence(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true)  // First entity passes
                         .Returns(false); // Second entity fails

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "", Value = 200, Validated = true }, // This will fail manual validation
            new TestEntity { Id = 3, Name = "Test3", Value = 300, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.False(result);
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(2)); // Should stop after failure
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never); // Should not reach summary validation
    }

    [Fact]
    public async Task ValidateManyAsync_WithSummaryValidationFails_ReturnsFalseAndStopsEarly()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Set up summary validation to fail on second entity
        mockValidationService.SetupSequence(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true)  // First entity passes
                           .ReturnsAsync(false); // Second entity fails

        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true },
            new TestEntity { Id = 3, Name = "Test3", Value = 300, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.False(result);
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(3)); // All manual validations should execute
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Should stop after failure
    }

    #endregion

    #region Sequence Validation Tests

    [Fact]
    public async Task ValidateManyAsync_WithSequenceValidationFails_ReturnsFalse()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var mockSummarisationPlanStore = new Mock<ISummarisationPlanStore>();
        var mockApplicationNameProvider = new Mock<IApplicationNameProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);
        
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Returns(new ValidationPlan(typeof(TestEntity), 5.0)); // Strict threshold

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns(mockEntityIdProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ISummarisationPlanStore)))
                         .Returns(mockSummarisationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                         .Returns(mockApplicationNameProvider.Object);

        // Set up entity ID provider to return different IDs for each entity
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                          .Returns<TestEntity>(entity => $"TestEntity{entity.Id}");

        mockApplicationNameProvider.Setup(p => p.ApplicationName)
                                  .Returns("Test");

        // Set up summarisation plan
        var mockSummarisationPlan = new Mock<SummarisationPlan<TestEntity>>(
            (TestEntity e) => e.Value,
            ThresholdType.RawDifference,
            10.0m);
        mockSummarisationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                                .Returns(true);
        mockSummarisationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                                .Returns(mockSummarisationPlan.Object);

        // Set up the DbContext with audit data that will cause validation to fail
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("test-bulk-sequence-validation-fail")
            .Options;

        using var realContext = new TheNannyDbContext(options);
        realContext.Database.EnsureCreated();
        
        // Add audit data that will cause validation to fail
        realContext.SaveAudits.Add(new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "Test",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await realContext.SaveChangesAsync();

        mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                         .Returns(realContext);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 200.0m, Validated = true }, // Large difference will cause failure
            new TestEntity { Id = 2, Name = "Test2", Value = 150.0m, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.False(result); // Should fail due to sequence validation
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(2)); // All manual validations should execute
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never); // Should not reach summary validation
    }

    [Fact]
    public async Task ValidateManyAsync_WithSequenceValidationPasses_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var mockSummarisationPlanStore = new Mock<ISummarisationPlanStore>();
        var mockApplicationNameProvider = new Mock<IApplicationNameProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);
        
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Returns(new ValidationPlan(typeof(TestEntity), 50.0)); // Generous threshold

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns(mockEntityIdProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ISummarisationPlanStore)))
                         .Returns(mockSummarisationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                         .Returns(mockApplicationNameProvider.Object);

        // Set up entity ID provider to return different IDs for each entity
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                          .Returns<TestEntity>(entity => $"TestEntity{entity.Id}");

        mockApplicationNameProvider.Setup(p => p.ApplicationName)
                                  .Returns("Test");

        // Set up summarisation plan
        var mockSummarisationPlan = new Mock<SummarisationPlan<TestEntity>>(
            (TestEntity e) => e.Value,
            ThresholdType.RawDifference,
            100.0m);
        mockSummarisationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                                .Returns(true);
        mockSummarisationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                                .Returns(mockSummarisationPlan.Object);

        // Set up the DbContext with audit data that will allow validation to pass
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("test-bulk-sequence-validation-pass")
            .Options;

        using var realContext = new TheNannyDbContext(options);
        realContext.Database.EnsureCreated();
        
        // Add audit data that will allow validation to pass
        realContext.SaveAudits.Add(new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "Test",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await realContext.SaveChangesAsync();

        mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                         .Returns(realContext);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 120.0m, Validated = true }, // Small difference will pass
            new TestEntity { Id = 2, Name = "Test2", Value = 110.0m, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result); // Should pass all validations
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(2));
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ValidateManyAsync_WithNoValidationPlan_SkipsSequenceValidation()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);
        
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(false); // No plan exists

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result);
        mockValidationPlanStore.Verify(s => s.HasPlan<TestEntity>(), Times.Once);
        mockValidationPlanStore.Verify(s => s.GetPlan<TestEntity>(), Times.Never);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ValidateManyAsync_WithExceptionInSequenceValidation_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);
        
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Throws(new InvalidOperationException("Test exception"));

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result); // Should pass due to graceful exception handling
    }

    [Fact]
    public async Task ValidateManyAsync_WithMissingServices_GracefullySkipsSequenceValidation()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);
        
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Returns(new ValidationPlan(typeof(TestEntity), 10.0));

        // Set up service provider to return the validation plan store but not other required services
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                         .Returns((TheNannyDbContext?)null); // Missing context
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns((IEntityIdProvider?)null); // Missing provider

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result); // Should still pass due to graceful degradation
        mockValidationPlanStore.Verify(s => s.HasPlan<TestEntity>(), Times.Once);
    }

    #endregion

    #region Execution Order Tests

    [Fact]
    public async Task ValidateManyAsync_ExecutesValidationsInCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .Callback<object, CancellationToken>((entity, _) => 
                           {
                               if (entity is TestEntity testEntity)
                                   executionOrder.Add($"Summary-{testEntity.Id}");
                           })
                           .ReturnsAsync(true);
        
        mockManualValidator.Setup(v => v.Validate(It.IsAny<object>()))
                         .Callback<object>(entity => 
                         {
                             if (entity is TestEntity testEntity)
                                 executionOrder.Add($"Manual-{testEntity.Id}");
                         })
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Callback(() => executionOrder.Add("Sequence"))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true }
        };

        // Act
        await runner.ValidateManyAsync(entities);

        // Assert
        Assert.Equal(5, executionOrder.Count);
        Assert.Equal("Manual-1", executionOrder[0]);
        Assert.Equal("Manual-2", executionOrder[1]);
        Assert.Equal("Sequence", executionOrder[2]);
        Assert.Equal("Summary-1", executionOrder[3]);
        Assert.Equal("Summary-2", executionOrder[4]);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task ValidateManyAsync_WithCancellationToken_PassesToServices()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var cancellationToken = new CancellationToken();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), cancellationToken))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Test1", Value = 100, Validated = true },
            new TestEntity { Id = 2, Name = "Test2", Value = 200, Validated = true }
        };

        // Act
        var result = await runner.ValidateManyAsync(entities, cancellationToken);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), cancellationToken), Times.Exactly(2));
    }

    #endregion

    #region Large Collection Tests

    [Fact]
    public async Task ValidateManyAsync_WithLargeCollection_HandlesEfficiently()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        // Create a large collection of entities
        var entities = new List<TestEntity>();
        for (int i = 1; i <= 1000; i++)
        {
            entities.Add(new TestEntity { Id = i, Name = $"Test{i}", Value = i * 10, Validated = true });
        }

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(1000));
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(1000));
    }

    [Fact]
    public async Task ValidateManyAsync_WithLargeCollectionAndEarlyFailure_StopsEarly()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        
        // Set up manual validator to fail on 10th entity
        var callCount = 0;
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(() => ++callCount != 10); // Fail on 10th call

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        // Create a large collection of entities
        var entities = new List<TestEntity>();
        for (int i = 1; i <= 1000; i++)
        {
            entities.Add(new TestEntity { Id = i, Name = $"Test{i}", Value = i * 10, Validated = true });
        }

        // Act
        var result = await runner.ValidateManyAsync(entities);

        // Assert
        Assert.False(result);
        mockManualValidator.Verify(v => v.Validate(It.IsAny<TestEntity>()), Times.Exactly(10)); // Should stop after 10th failure
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never); // Should not reach summary validation
    }

    #endregion

    #region Single Entity Compatibility Tests

    [Fact]
    public async Task ValidateManyAsync_WithSingleEntity_BehavesLikeValidateAsync()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Id = 1, Name = "Test", Value = 100, Validated = true };

        // Act
        var singleResult = await runner.ValidateAsync(entity);
        var bulkResult = await runner.ValidateManyAsync(new[] { entity });

        // Assert
        Assert.Equal(singleResult, bulkResult);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(entity, It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockManualValidator.Verify(v => v.Validate(entity), Times.Exactly(2));
    }

    #endregion
}