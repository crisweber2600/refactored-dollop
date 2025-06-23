namespace MetricsPipeline.Infrastructure;

using System;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring setup validation services.
/// </summary>
public static class SetupValidationExtensions
{
    /// <summary>
    /// Registers the validation services and performs setup using the supplied options.
    /// </summary>
    public static IServiceCollection SetupValidation(this IServiceCollection services, Action<SetupValidationOptions> configure)
    {
        var options = new SetupValidationOptions();
        configure(options);
        services.AddSingleton(options);

        var validator = new SetupValidator(options);
        validator.Setup(services);
        services.AddTransient<SetupValidator>(_ => new SetupValidator(options));
        return services;
    }

    /// <summary>
    /// Registers a typed validator that relies on the configured services.
    /// </summary>
    public static IServiceCollection AddSetupValidation<T>(this IServiceCollection services) where T : SetupValidator
    {
        services.AddTransient<T>();
        return services;
    }
}
