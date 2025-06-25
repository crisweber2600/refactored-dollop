using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Extension methods for wiring up <see cref="ManualValidatorService"/>.
/// </summary>
public static class ManualValidatorExtensions
{
    private static IDictionary<Type, List<Func<object, bool>>>? _rules;

    /// <summary>
    /// Registers <see cref="IManualValidatorService"/> and the underlying rule dictionary.
    /// </summary>
    public static IServiceCollection AddValidatorService(this IServiceCollection services)
    {
        _rules = new Dictionary<Type, List<Func<object, bool>>>();
        services.AddSingleton<IDictionary<Type, List<Func<object, bool>>>>(_rules);
        services.AddSingleton<IManualValidatorService, ManualValidatorService>();
        return services;
    }

    /// <summary>
    /// Adds a validation rule for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddValidatorRule<T>(this IServiceCollection services, Func<T, bool> rule)
    {
        _rules ??= new Dictionary<Type, List<Func<object, bool>>>();
        if (!_rules.TryGetValue(typeof(T), out var list))
        {
            list = new List<Func<object, bool>>();
            _rules[typeof(T)] = list;
        }
        list.Add(o => rule((T)o));
        return services;
    }
}
