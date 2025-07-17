using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Comprehensive unit tests for ValidationRunner class.
/// Tests the core validation orchestration logic.
/// </summary>
public class ValidationRunnerUnitTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public async Task ValidateAsync_WithAllValidationsPass_ReturnsTrue()
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

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        mockManualValidator.Verify(v => v.Validate(entity), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithSummaryValidationFails_ReturnsFalse()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(false); // Summary validation fails
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.False(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        mockManualValidator.Verify(v => v.Validate(entity), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithManualValidationFails_ReturnsFalse()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(false); // Manual validation fails

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.False(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        mockManualValidator.Verify(v => v.Validate(entity), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithoutValidationPlan_SkipsSequenceValidation()
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

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
        mockValidationPlanStore.Verify(s => s.HasPlan<TestEntity>(), Times.Once);
        mockValidationPlanStore.Verify(s => s.GetPlan<TestEntity>(), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WithValidationPlanButMissingServices_GracefullySkipsSequenceValidation()
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

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result); // Should still pass due to graceful degradation
        mockValidationPlanStore.Verify(s => s.HasPlan<TestEntity>(), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithSequenceValidationFails_ReturnsFalse()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockDbContext = new Mock<TheNannyDbContext>(
            new DbContextOptionsBuilder<TheNannyDbContext>()
                .UseInMemoryDatabase("test-sequence-fail")
                .Options);
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
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                         .Returns(mockDbContext.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns(mockEntityIdProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ISummarisationPlanStore)))
                         .Returns(mockSummarisationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                         .Returns(mockApplicationNameProvider.Object);

        // Set up entity ID provider
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                          .Returns("TestEntity1");

        // Set up application name provider
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
            .UseInMemoryDatabase("test-sequence-validation-fail")
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

        // Replace the mock with the real context for this test
        mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                         .Returns(realContext);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 150.0m, Validated = true }; // Large difference

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.False(result); // Should fail due to sequence validation
    }

    [Fact]
    public async Task ValidateAsync_WithSequenceValidationPasses_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var mockSummarisationPlanStore = new Mock<ISummarisationPlanStore>();

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

        // Set up entity ID provider
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                          .Returns("TestEntity1");

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
            .UseInMemoryDatabase("test-sequence-validation-pass")
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

        var entity = new TestEntity { Name = "Test", Value = 120.0m, Validated = true }; // Small difference

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result); // Should pass all validations
    }

    [Fact]
    public async Task ValidateAsync_WithExceptionInSequenceValidation_ReturnsTrue()
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

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result); // Should pass due to graceful exception handling
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_PassesToServices()
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

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity, cancellationToken);

        // Assert
        Assert.True(result);
        mockValidationService.Verify(v => v.ValidateAndSaveAsync(entity, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithNullValidationPlan_SkipsSequenceValidation()
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
                              .Returns((ValidationPlan?)null); // Plan exists but returns null

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
        mockValidationPlanStore.Verify(s => s.GetPlan<TestEntity>(), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_AllValidationTypesExecuted_InCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .Callback(() => executionOrder.Add("Summary"))
                           .ReturnsAsync(true);
        
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Callback(() => executionOrder.Add("Manual"))
                         .Returns(true);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Callback(() => executionOrder.Add("Sequence"))
                         .Returns((IValidationPlanStore?)null);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        await runner.ValidateAsync(entity);

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("Manual", executionOrder[0]);
        Assert.Equal("Sequence", executionOrder[1]);
        Assert.Equal("Summary", executionOrder[2]);
    }

    [Theory]
    [InlineData(true, true, true, true)]   // All pass
    [InlineData(false, true, true, false)] // Summary fails
    [InlineData(true, false, true, false)] // Manual fails
    [InlineData(true, true, false, false)] // Sequence fails
    [InlineData(false, false, false, false)] // All fail
    public async Task ValidateAsync_CombinedResults_ReturnsCorrectOverallResult(
        bool summaryResult, bool manualResult, bool sequenceResult, bool expectedResult)
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
                           .ReturnsAsync(summaryResult);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(manualResult);

        if (!sequenceResult)
        {
            // Set up sequence validation to fail
            mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                                  .Returns(true);
            mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                                  .Returns(new ValidationPlan(typeof(TestEntity), 5.0));

            mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                             .Returns(mockValidationPlanStore.Object);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                             .Returns(mockEntityIdProvider.Object);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(ISummarisationPlanStore)))
                             .Returns(mockSummarisationPlanStore.Object);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                             .Returns(mockApplicationNameProvider.Object);

            mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                              .Returns("TestEntity1");

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

            // Set up DbContext with data that will cause sequence validation to fail
            var options = new DbContextOptionsBuilder<TheNannyDbContext>()
                .UseInMemoryDatabase($"test-combined-{summaryResult}-{manualResult}-{sequenceResult}")
                .Options;

            var context = new TheNannyDbContext(options);
            context.Database.EnsureCreated();
            context.SaveAudits.Add(new SaveAudit
            {
                EntityType = nameof(TestEntity),
                EntityId = "TestEntity1",
                ApplicationName = "Test",
                MetricValue = 100.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await context.SaveChangesAsync();

            mockServiceProvider.Setup(sp => sp.GetService(typeof(TheNannyDbContext)))
                             .Returns(context);
        }
        else
        {
            // Skip sequence validation or make it pass
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                             .Returns((IValidationPlanStore?)null);
        }

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity 
        { 
            Name = "Test", 
            Value = sequenceResult ? 105.0m : 200.0m, // Large diff for sequence failure
            Validated = true 
        };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}