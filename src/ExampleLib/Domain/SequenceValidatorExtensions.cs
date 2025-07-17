using Microsoft.EntityFrameworkCore;
using ExampleLib.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleLib.Domain;

/// <summary>
/// Extension methods for SequenceValidator to integrate with IEntityIdProvider.
/// </summary>
public static class SequenceValidatorExtensions
{
    /// <summary>
    /// Validates a sequence by comparing each entity's value to the latest audit value from a DbSet,
    /// automatically using the registered EntityId selector from IEntityIdProvider.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="entities">Entities to validate</param>
    /// <param name="audits">DbSet of SaveAudit records</param>
    /// <param name="entityIdProvider">EntityId provider for determining the correct key mapping</param>
    /// <param name="valueSelector">Selects the value from the entity</param>
    /// <param name="auditValueSelector">Selects the value from the audit (typically a => a.MetricValue)</param>
    /// <param name="validationFunc">Compares entity value to audit value</param>
    /// <param name="applicationName">Application name to filter audits by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all entities are valid, otherwise false</returns>
    public static async Task<bool> ValidateAgainstAuditsWithProviderAsync<T, TValue>(
        IEnumerable<T> entities,
        DbSet<SaveAudit> audits,
        IEntityIdProvider entityIdProvider,
        Func<T, TValue> valueSelector,
        Func<SaveAudit, TValue> auditValueSelector,
        Func<TValue, TValue, bool> validationFunc,
        string applicationName,
        CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        // Validate arguments
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (audits == null) throw new ArgumentNullException(nameof(audits));
        if (entityIdProvider == null) throw new ArgumentNullException(nameof(entityIdProvider));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (auditValueSelector == null) throw new ArgumentNullException(nameof(auditValueSelector));
        if (validationFunc == null) throw new ArgumentNullException(nameof(validationFunc));
        if (string.IsNullOrWhiteSpace(applicationName)) throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));

        return await SequenceValidator.ValidateAgainstSaveAuditsAsync(
            entities,
            audits,
            entity => entityIdProvider.GetEntityId(entity) ?? throw new InvalidOperationException($"EntityIdProvider returned null for entity of type {typeof(T).Name}"),
            valueSelector,
            auditValueSelector,
            validationFunc,
            applicationName,
            cancellationToken
        );
    }

    /// <summary>
    /// Validates a sequence using a ValidationPlan and IEntityIdProvider, comparing against SaveAudit records.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="entities">Entities to validate</param>
    /// <param name="audits">DbSet of SaveAudit records</param>
    /// <param name="entityIdProvider">EntityId provider for determining the correct key mapping</param>
    /// <param name="plan">ValidationPlan containing threshold logic</param>
    /// <param name="valueSelector">Selects the value from the entity to compare</param>
    /// <param name="applicationName">Application name to filter audits by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all entities are valid according to the plan's threshold, otherwise false</returns>
    public static async Task<bool> ValidateWithPlanAndProviderAsync<T>(
        IEnumerable<T> entities,
        DbSet<SaveAudit> audits,
        IEntityIdProvider entityIdProvider,
        ValidationPlan plan,
        Func<T, decimal> valueSelector,
        string applicationName,
        CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        // Validate arguments
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (audits == null) throw new ArgumentNullException(nameof(audits));
        if (entityIdProvider == null) throw new ArgumentNullException(nameof(entityIdProvider));
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (string.IsNullOrWhiteSpace(applicationName)) throw new ArgumentException("Application name cannot be null or empty", nameof(applicationName));

        Console.WriteLine($"Debug ValidateWithPlanAndProviderAsync: Starting with threshold {plan.Threshold}, appName {applicationName}");

        return await ValidateAgainstAuditsWithProviderAsync(
            entities,
            audits,
            entityIdProvider,
            valueSelector,
            audit => audit.MetricValue,
            (newValue, auditValue) => {
                var difference = Math.Abs(newValue - auditValue);
                var threshold = (decimal)plan.Threshold;
                var isValid = difference <= threshold;
                Console.WriteLine($"Debug ValidateWithPlanAndProviderAsync: newValue={newValue}, auditValue={auditValue}, difference={difference}, threshold={threshold}, isValid={isValid}");
                return isValid;
            },
            applicationName,
            cancellationToken
        );
    }
}