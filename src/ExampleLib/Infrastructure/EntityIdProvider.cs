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
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        _selectors[typeof(T)] = obj => selector((T)obj);
    }

    /// <inheritdoc />
    public string GetEntityId<T>(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        
        var entityType = typeof(T);
        if (_selectors.TryGetValue(entityType, out var selector))
        {
            return selector(entity);
        }

        // If no selector is registered, throw an exception
        throw new InvalidOperationException($"No selector registered for type {entityType.Name}");
    }
}

/// <summary>
/// Reflection-based entity ID provider that automatically discovers suitable string properties.
/// </summary>
public class ReflectionBasedEntityIdProvider : IEntityIdProvider
{
    private readonly string[] _propertyPriority;
    private readonly Dictionary<Type, PropertyInfo[]> _cachedProperties = new();

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
        if (propertyPriority == null)
            throw new ArgumentNullException(nameof(propertyPriority));
        
        _propertyPriority = propertyPriority.Length > 0 ? propertyPriority : new[] { "Name", "Code", "Key", "Identifier", "Title", "Label" };
    }

    /// <inheritdoc />
    public string GetEntityId<T>(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var entityType = typeof(T);
        if (!_cachedProperties.TryGetValue(entityType, out var properties))
        {
            properties = FindPropertiesInPriorityOrder(entityType);
            _cachedProperties[entityType] = properties;
        }

        // Try each property in order until we find a valid value
        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            var stringValue = value?.ToString();
            
            // Return the first non-null, non-empty, non-whitespace value
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }

        // If no valid property found or all values are invalid, fall back to ToString()
        return entity.ToString() ?? string.Empty;
    }

    private PropertyInfo[] FindPropertiesInPriorityOrder(Type entityType)
    {
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .ToList();

        var result = new List<PropertyInfo>();

        // Add properties based on priority list with case-sensitive matching
        foreach (var priorityName in _propertyPriority)
        {
            var property = properties.FirstOrDefault(p => 
                string.Equals(p.Name, priorityName, StringComparison.Ordinal));
            if (property != null)
            {
                result.Add(property);
            }
        }

        // If no priority properties found, no fallback to other properties
        // This matches the expected behavior from the tests
        
        return result.ToArray();
    }
}