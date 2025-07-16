using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Domain;

/// <summary>
/// Provides helpers for validating ordered sequences of items based on dynamic selectors.
/// </summary>
public static class SequenceValidator
{
    /// <summary>
    /// Validates a sequence by comparing the selected value of each item to the most
    /// recent prior item with the **same** discriminator key.
    /// </summary>
    /// <param name="items">Items to validate in order.</param>
    /// <param name="wheneverSelector">Selects a discriminator key.</param>
    /// <param name="valueSelector">Selects the value used for comparison.</param>
    /// <param name="validationFunc">Determines if the current value is valid compared to the previous.</param>
    public static bool Validate<T, TKey, TValue>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        Func<T, TValue> valueSelector,
        Func<TValue, TValue, bool> validationFunc)
        where TKey : notnull
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (wheneverSelector == null) throw new ArgumentNullException(nameof(wheneverSelector));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (validationFunc == null) throw new ArgumentNullException(nameof(validationFunc));

        var lastValues = new Dictionary<TKey, TValue>();

        foreach (var item in items)
        {
            var key = wheneverSelector(item);
            var value = valueSelector(item);

            if (lastValues.TryGetValue(key, out var previous))
            {
                if (!validationFunc(value, previous))
                    return false;
            }

            lastValues[key] = value;
        }

        return true;
    }

    /// <summary>
    /// Validates a sequence using default equality comparison on the selected value.
    /// </summary>
    public static bool Validate<T, TKey, TValue>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        return Validate(items, wheneverSelector, valueSelector, (c, p) => EqualityComparer<TValue>.Default.Equals(c, p));
    }

    /// <summary>
    /// Validates a sequence using a <see cref="SummarisationPlan{T}"/>. Metric values
    /// are compared according to the plan's threshold rules.
    /// </summary>
    public static bool Validate<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        SummarisationPlan<T> plan)
        where TKey : notnull
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        return Validate(items, wheneverSelector, plan.MetricSelector, (cur, prev) =>
            ThresholdValidator.IsWithinThreshold(
                cur,
                prev,
                plan.ThresholdType,
                plan.ThresholdValue,
                throwOnUnsupported: true));
    }

    /// <summary>
    /// Validates a sequence by comparing each entity's value to the latest audit value from a DbSet for the same key.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TAudit">Audit type</typeparam>
    /// <typeparam name="TKey">Key type (whenever value)</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="entities">Entities to validate</param>
    /// <param name="audits">DbSet of audits</param>
    /// <param name="keySelector">Selects the key from the entity</param>
    /// <param name="auditKeySelector">Selects the key from the audit</param>
    /// <param name="valueSelector">Selects the value from the entity</param>
    /// <param name="auditValueSelector">Selects the value from the audit</param>
    /// <param name="validationFunc">Compares entity value to audit value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all entities are valid, otherwise false</returns>
    public static async Task<bool> ValidateAgainstLatestAuditAsync<T, TAudit, TKey, TValue>(
        IEnumerable<T> entities,
        DbSet<TAudit> audits,
        Func<T, TKey> keySelector,
        Func<TAudit, TKey> auditKeySelector,
        Func<T, TValue> valueSelector,
        Func<TAudit, TValue> auditValueSelector,
        Func<TValue, TValue, bool> validationFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TAudit : class // Ensure TAudit is a reference type for DbSet
    {
        foreach (var entity in entities)
        {
            var key = keySelector(entity);
            var latestAudit = await audits
                .Where(a => auditKeySelector(a).Equals(key))
                .OrderByDescending(a => EF.Property<DateTimeOffset>(a!, "Timestamp"))
                .FirstOrDefaultAsync(cancellationToken);

            if (latestAudit != null)
            {
                var entityValue = valueSelector(entity);
                var auditValue = auditValueSelector(latestAudit);
                if (!validationFunc(entityValue, auditValue))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates a sequence of new entities by comparing each entity's value to the latest existing entity value from the database for the same discriminator key.
    /// For example: Database has [{Name:"A",Value:7},{Name:"B",Value:4}], new list has [{Name:"A",Value:2},{Name:"B",Value:7}].
    /// For Name="A": compares new Value=2 against existing Value=7 using the validation function.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Discriminator key type (e.g., Name)</typeparam>
    /// <typeparam name="TValue">Value type to compare (e.g., Value property)</typeparam>
    /// <param name="newEntities">New entities to validate before insertion</param>
    /// <param name="existingEntities">DbSet of existing entities in the database</param>
    /// <param name="keySelector">Selects the discriminator key (e.g., Name)</param>
    /// <param name="valueSelector">Selects the value to compare (e.g., Value property)</param>
    /// <param name="validationFunc">Compares new value against existing value (e.g., using plan threshold)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all new entities are valid against existing database values, otherwise false</returns>
    public static async Task<bool> ValidateAgainstExistingEntitiesAsync<T, TKey, TValue>(
        IEnumerable<T> newEntities,
        DbSet<T> existingEntities,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        Func<TValue, TValue, bool> validationFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where T : class
    {
        foreach (var newEntity in newEntities)
        {
            var key = keySelector(newEntity);
            var newValue = valueSelector(newEntity);
            
            // Find the latest existing entity with the same discriminator key (order by Id as the primary key)
            var latestExisting = await existingEntities
                .Where(e => keySelector(e).Equals(key))
                .OrderByDescending(e => EF.Property<int>(e!, "Id"))
                .FirstOrDefaultAsync(cancellationToken);

            if (latestExisting != null)
            {
                var existingValue = valueSelector(latestExisting);
                if (!validationFunc(newValue, existingValue))
                    return false;
            }
            // If no existing entity found, validation passes (first time inserting this key)
        }
        return true;
    }
}
