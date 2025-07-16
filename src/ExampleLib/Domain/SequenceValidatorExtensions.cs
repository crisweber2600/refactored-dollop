using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ExampleLib.Infrastructure; // Add this for IEntityIdProvider
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
    /// Registers a ConfigurableEntityIdProvider with custom selectors for entity types.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Action to configure the EntityId selectors</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddConfigurableEntityIdProvider(
        this IServiceCollection services, 
        Action<ConfigurableEntityIdProvider> configure)
    {
        var provider = new ConfigurableEntityIdProvider();
        configure(provider);
        services.AddSingleton<IEntityIdProvider>(provider);
        return services;
    }

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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all entities are valid, otherwise false</returns>
    public static async Task<bool> ValidateAgainstAuditsWithProviderAsync<T, TValue>(
        IEnumerable<T> entities,
        DbSet<SaveAudit> audits,
        IEntityIdProvider entityIdProvider,
        Func<T, TValue> valueSelector,
        Func<SaveAudit, TValue> auditValueSelector,
        Func<TValue, TValue, bool> validationFunc,
        CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        return await SequenceValidator.ValidateAgainstLatestAuditAsync<T, SaveAudit, string, TValue>(
            entities,
            audits,
            entity => entityIdProvider.GetEntityId(entity), // Use EntityIdProvider for key extraction
            audit => audit.EntityId, // SaveAudit.EntityId contains the discriminator key
            valueSelector,
            auditValueSelector,
            validationFunc,
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all entities are valid according to the plan's threshold, otherwise false</returns>
    public static async Task<bool> ValidateWithPlanAndProviderAsync<T>(
        IEnumerable<T> entities,
        DbSet<SaveAudit> audits,
        IEntityIdProvider entityIdProvider,
        ValidationPlan plan,
        Func<T, decimal> valueSelector,
        CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        return await ValidateAgainstAuditsWithProviderAsync(
            entities,
            audits,
            entityIdProvider,
            valueSelector,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= (decimal)plan.Threshold,
            cancellationToken
        );
    }
}