using System.Reflection;
using ExampleLib.Domain;
using MassTransit;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Entity repository that publishes SaveRequested events for validation on save.
/// </summary>
public class EventPublishingRepository<T> : IEntityRepository<T>
{
    private readonly IBus _bus;

    public EventPublishingRepository(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public Task SaveAsync(string appName, T entity)
    {
        var entityId = GetEntityId(entity);
        var saveEvent = new SaveRequested<T>
        {
            AppName = appName,
            EntityType = typeof(T).Name,
            EntityId = entityId,
            Payload = entity
        };
        return _bus.Publish(saveEvent);
    }

    private string GetEntityId(T entity)
    {
        if (entity == null) return string.Empty;
        var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProp != null)
        {
            var value = idProp.GetValue(entity);
            if (value != null)
                return value.ToString()!;
        }
        return Guid.NewGuid().ToString();
    }
}
