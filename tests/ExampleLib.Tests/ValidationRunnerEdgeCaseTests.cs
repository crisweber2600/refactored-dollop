using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ValidationRunner edge cases and GetDefaultValueSelector to improve coverage.
/// These tests focus on the remaining uncovered code paths in ValidationRunner.
/// </summary>
public class ValidationRunnerEdgeCaseTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public async Task ValidateSequenceAsync_WithValidationPlanStoreException_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();

        // Set up the validation service and manual validator to pass
        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        // Set up the validation plan store to throw an exception
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Throws(new InvalidOperationException("ValidationPlanStore exception"));

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle exception gracefully and return true
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSequenceAsync_WithSequenceValidatorException_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var mockApplicationNameProvider = new Mock<IApplicationNameProvider>();

        // Set up the validation service and manual validator to pass
        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        // Set up validation plan store to have a plan
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);

        var plan = new ValidationPlan(typeof(TestEntity), 10.0, ValidationStrategy.Average);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Returns(plan);

        // Set up entity id provider to throw an exception
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                           .Throws(new InvalidOperationException("EntityIdProvider exception"));

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns(mockEntityIdProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                         .Returns(mockApplicationNameProvider.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle exception gracefully and return true
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSequenceAsync_WithSequenceValidatorExceptionInAsyncCall_ReturnsTrue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var mockApplicationNameProvider = new Mock<IApplicationNameProvider>();
        var mockSaveAuditRepository = new Mock<ISaveAuditRepository>();

        // Set up the validation service and manual validator to pass
        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);
        mockManualValidator.Setup(v => v.Validate(It.IsAny<TestEntity>()))
                         .Returns(true);

        // Set up validation plan store to have a plan
        mockValidationPlanStore.Setup(s => s.HasPlan<TestEntity>())
                              .Returns(true);

        var plan = new ValidationPlan(typeof(TestEntity), 10.0, ValidationStrategy.Average);
        mockValidationPlanStore.Setup(s => s.GetPlan<TestEntity>())
                              .Returns(plan);

        // Set up entity id provider and application name provider
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
                           .Returns("TestEntity1");
        mockApplicationNameProvider.Setup(p => p.ApplicationName)
                                  .Returns("TestApp");

        // Set up save audit repository to throw an exception
        mockSaveAuditRepository.Setup(r => r.GetLastAudit(It.IsAny<string>(), It.IsAny<string>()))
                              .Throws(new InvalidOperationException("SaveAuditRepository exception"));

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidationPlanStore)))
                         .Returns(mockValidationPlanStore.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEntityIdProvider)))
                         .Returns(mockEntityIdProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationNameProvider)))
                         .Returns(mockApplicationNameProvider.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ISaveAuditRepository)))
                         .Returns(mockSaveAuditRepository.Object);

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle exception gracefully and return true
        Assert.True(result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithValidEntity_ReturnsCorrectValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act
        var selector = runner.GetDefaultValueSelector<TestEntity>();
        var result = selector(entity);

        // Assert
        Assert.Equal(100m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        // Act
        var selector = runner.GetDefaultValueSelector<TestEntity>();

        // Assert
        Assert.Throws<ArgumentNullException>(() => selector(null!));
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidAsyncMethod_ThrowsCorrectException()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Set up validation service to throw an exception
        mockValidationService.Setup(v => v.ValidateAndSaveAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new InvalidOperationException("Validation failed"));

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new TestEntity { Name = "Test", Value = 100, Validated = true };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ValidateAsync(entity));
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithoutDecimalProperty_ReturnsIdValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithoutDecimalProperty { Name = "Test", Id = 42, Validated = true };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithoutDecimalProperty>();
        var result = selector(entity);

        // Assert
        Assert.Equal(42m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithMultipleDecimalProperties_ReturnsFirst()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithMultipleDecimalProperties 
        { 
            Name = "Test", 
            Id = 1, 
            Amount = 50m, 
            Value = 100m,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithMultipleDecimalProperties>();
        var result = selector(entity);

        // Assert
        Assert.Equal(100m, result); // Should return the Value property first
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithAmountOnly_ReturnsAmount()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithAmountOnly 
        { 
            Name = "Test", 
            Id = 1, 
            Amount = 75m,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithAmountOnly>();
        var result = selector(entity);

        // Assert
        Assert.Equal(75m, result); // Should return the Amount property
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithNullableDecimalProperty_ReturnsCorrectValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithNullableDecimalProperty 
        { 
            Name = "Test", 
            Id = 1, 
            Value = 75m,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithNullableDecimalProperty>();
        var result = selector(entity);

        // Assert
        Assert.Equal(75m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithNullNullableDecimalProperty_ReturnsZero()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithNullableDecimalProperty 
        { 
            Name = "Test", 
            Id = 1, 
            Value = null,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithNullableDecimalProperty>();
        var result = selector(entity);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithIntValue_ReturnsCorrectValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithIntValue 
        { 
            Name = "Test", 
            Id = 1, 
            Value = 123,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithIntValue>();
        var result = selector(entity);

        // Assert
        Assert.Equal(123m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithDoubleValue_ReturnsCorrectValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithDoubleValue 
        { 
            Name = "Test", 
            Id = 1, 
            Value = 123.45,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithDoubleValue>();
        var result = selector(entity);

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void GetDefaultValueSelector_WithEntityWithFloatValue_ReturnsCorrectValue()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockManualValidator = new Mock<IManualValidatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        var runner = new ValidationRunner(
            mockValidationService.Object,
            mockManualValidator.Object,
            mockServiceProvider.Object);

        var entity = new EntityWithFloatValue 
        { 
            Name = "Test", 
            Id = 1, 
            Value = 123.45f,
            Validated = true
        };

        // Act
        var selector = runner.GetDefaultValueSelector<EntityWithFloatValue>();
        var result = selector(entity);

        // Assert
        Assert.Equal(123.45m, result);
    }

    // Helper entity classes
    public class EntityWithoutDecimalProperty : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }

    public class EntityWithMultipleDecimalProperties : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithAmountOnly : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableDecimalProperty : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithIntValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithDoubleValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithFloatValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public float Value { get; set; }
        public bool Validated { get; set; }
    }
}