namespace ExampleLib.Infrastructure;

/// <summary>
/// Configuration options for ExampleLib validation system.
/// </summary>
public class ExampleLibOptions
{
    /// <summary>
    /// Application name used in SaveAudit records.
    /// </summary>
    public string ApplicationName { get; set; } = "DefaultApp";

    /// <summary>
    /// Whether to use MongoDB implementations by default.
    /// </summary>
    public bool UseMongoDb { get; set; } = false;

    /// <summary>
    /// Default threshold type for summarisation validation.
    /// </summary>
    public ExampleLib.Domain.ThresholdType DefaultThresholdType { get; set; } = ExampleLib.Domain.ThresholdType.PercentChange;

    /// <summary>
    /// Default threshold value for summarisation validation.
    /// </summary>
    public decimal DefaultThresholdValue { get; set; } = 0.1m;

    /// <summary>
    /// Entity ID provider configuration.
    /// </summary>
    public EntityIdProviderOptions EntityIdProvider { get; set; } = new();

    /// <summary>
    /// MongoDB-specific configuration.
    /// </summary>
    public MongoDbOptions MongoDb { get; set; } = new();

    /// <summary>
    /// Entity Framework-specific configuration.
    /// </summary>
    public EntityFrameworkOptions EntityFramework { get; set; } = new();
}

/// <summary>
/// Configuration options for EntityIdProvider.
/// </summary>
public class EntityIdProviderOptions
{
    /// <summary>
    /// Type of EntityIdProvider to use.
    /// </summary>
    public EntityIdProviderType Type { get; set; } = EntityIdProviderType.Reflection;

    /// <summary>
    /// Property names to search for in priority order (for ReflectionBasedEntityIdProvider).
    /// </summary>
    public string[] PropertyPriority { get; set; } = { "Name", "Code", "Key", "Identifier", "Title", "Label" };
}

/// <summary>
/// Types of EntityIdProvider available.
/// </summary>
public enum EntityIdProviderType
{
    /// <summary>Use reflection-based discovery of string properties.</summary>
    Reflection,
    /// <summary>Use manually configured selectors.</summary>
    Configurable,
    /// <summary>Use default Id.ToString() approach.</summary>
    Default
}

/// <summary>
/// MongoDB-specific configuration options.
/// </summary>
public class MongoDbOptions
{
    /// <summary>
    /// Default database name for repositories.
    /// </summary>
    public string DefaultDatabaseName { get; set; } = "ExampleLibDb";

    /// <summary>
    /// Collection naming strategy function.
    /// </summary>
    public Func<Type, string> CollectionNamingStrategy { get; set; } = type => $"{type.Name}s";
}

/// <summary>
/// Entity Framework-specific configuration options.
/// </summary>
public class EntityFrameworkOptions
{
    /// <summary>
    /// Whether to automatically register repositories for all entity types found in the context.
    /// </summary>
    public bool AutoRegisterRepositories { get; set; } = false;
}