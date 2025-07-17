using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for TheNannyDbContextFactory to improve code coverage.
/// </summary>
public class TheNannyDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WithNullArgs_CreatesContextWithDefaultConnectionString()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context = factory.CreateDbContext(null!);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // The context should be usable
        Assert.NotNull(context.SaveAudits);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithEmptyArgs_CreatesContextWithDefaultConnectionString()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();
        var args = new string[0];

        // Act
        var context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // The context should be usable
        Assert.NotNull(context.SaveAudits);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithArgs_CreatesContextWithDefaultConnectionString()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();
        var args = new[] { "arg1", "arg2" };

        // Act
        var context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // The context should be usable
        Assert.NotNull(context.SaveAudits);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_CreatesContextWithInMemoryDatabase()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context = factory.CreateDbContext(new string[0]);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // Verify it's using in-memory database
        Assert.Contains("InMemory", context.Database.ProviderName!);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_MultipleCallsCreateSeparateContexts()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context1 = factory.CreateDbContext(new string[0]);
        var context2 = factory.CreateDbContext(new string[0]);

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2);
        
        // Both should be valid contexts
        Assert.IsType<TheNannyDbContext>(context1);
        Assert.IsType<TheNannyDbContext>(context2);
        
        // Clean up
        context1.Dispose();
        context2.Dispose();
    }

    [Fact]
    public void CreateDbContext_ContextCanBeUsedForDatabaseOperations()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context = factory.CreateDbContext(new string[0]);
        
        // Assert
        Assert.NotNull(context);
        
        // Should be able to create the database
        context.Database.EnsureCreated();
        
        // Should be able to access SaveAudits
        Assert.NotNull(context.SaveAudits);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_UsesCorrectDatabaseProvider()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context = factory.CreateDbContext(new string[0]);

        // Assert
        Assert.NotNull(context);
        
        // Should be using InMemory provider
        Assert.Contains("InMemory", context.Database.ProviderName!);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_CreatedContextHasCorrectModelConfiguration()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();

        // Act
        var context = factory.CreateDbContext(new string[0]);

        // Assert
        Assert.NotNull(context);
        
        // Should have SaveAudits configured
        var entityType = context.Model.FindEntityType(typeof(Domain.SaveAudit));
        Assert.NotNull(entityType);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithLongArgs_StillCreatesValidContext()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();
        var args = new[] { 
            "very", "long", "list", "of", "arguments", 
            "to", "test", "that", "the", "factory", 
            "handles", "many", "arguments", "correctly" 
        };

        // Act
        var context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // Clean up
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithSpecialCharactersInArgs_StillCreatesValidContext()
    {
        // Arrange
        var factory = new TheNannyDbContextFactory();
        var args = new[] { 
            "arg with spaces", 
            "arg-with-dashes", 
            "arg_with_underscores", 
            "arg.with.dots",
            "arg@with@symbols"
        };

        // Act
        var context = factory.CreateDbContext(args);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TheNannyDbContext>(context);
        
        // Clean up
        context.Dispose();
    }
}