using System.Reflection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Configurable entity ID provider that uses registered selectors for specific entity types.
/// </summary>
public class ConfigurableEntityIdProvider : IEntityIdProvider
{
    private readonly Dictionary<Type, Func<object, string>> _selectors = new();

    /// <summary>
    /// Register a custom selector for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="selector">Function to extract the ID value from the entity</param>
    public void RegisterSelector<T>(Func<T, string> selector)
    {
        _selectors[typeof(T)] = obj => selector((T)obj);
    }

    /// <inheritdoc />
    public string GetEntityId<T>(T entity)
    {
        if (entity == null) return string.Empty;
        
        var entityType = typeof(T);
        if (_selectors.TryGetValue(entityType, out var selector))
        {
            return selector(entity);
        }

        // Fallback to Id property if available
        var idProperty = entityType.GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}

/// <summary>
/// Reflection-based entity ID provider that automatically discovers suitable string properties.
/// </summary>
public class ReflectionBasedEntityIdProvider : IEntityIdProvider
{
    private readonly string[] _propertyPriority;
    private readonly Dictionary<Type, PropertyInfo?> _cachedProperties = new();

    /// <summary>
    /// Create a reflection-based provider with default property priority.
    /// </summary>
    public ReflectionBasedEntityIdProvider() 
        : this("Name", "Code", "Key", "Identifier", "Title", "Label")
    {
    }

    /// <summary>
    /// Create a reflection-based provider with custom property priority.
    /// </summary>
    /// <param name="propertyPriority">Property names in order of priority</param>
    public ReflectionBasedEntityIdProvider(params string[] propertyPriority)
    {
        _propertyPriority = propertyPriority;
    }

    /// <inheritdoc />
    public string GetEntityId<T>(T entity)
    {
        if (entity == null) return string.Empty;

        var entityType = typeof(T);
        if (!_cachedProperties.TryGetValue(entityType, out var property))
        {
            property = FindBestProperty(entityType);
            _cachedProperties[entityType] = property;
        }

        if (property != null)
        {
            var value = property.GetValue(entity);
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private PropertyInfo? FindBestProperty(Type entityType)
    {
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .ToList();

        // First try the priority list
        foreach (var priorityName in _propertyPriority)
        {
            var property = properties.FirstOrDefault(p => 
                string.Equals(p.Name, priorityName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
                return property;
        }

        // Then try any other suitable string property
        return properties.FirstOrDefault();
    }
}