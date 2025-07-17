using System.Linq.Expressions;
using System.Reflection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Provides a default selector for entities that have an 'Id' property.
/// </summary>
public static class DefaultSelector
{
    /// <summary>
    /// Creates a selector expression for the 'Id' property of an entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <returns>An expression that selects the 'Id' property.</returns>
    public static Expression<Func<T, object>> Create<T>()
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, "Id");
        var conversion = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<T, object>>(conversion, parameter);
    }
}
