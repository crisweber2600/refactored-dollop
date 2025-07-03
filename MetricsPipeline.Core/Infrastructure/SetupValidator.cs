namespace MetricsPipeline.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;

/// <summary>
/// Performs a multi step setup process when validating the host configuration.
/// </summary>
public class SetupValidator
{
    /// <summary>Options controlling registration.</summary>
    protected readonly SetupValidationOptions Options;

    /// <summary>Creates the validator with the supplied options.</summary>
    public SetupValidator(SetupValidationOptions options)
    {
        Options = options;
    }

    /// <summary>Registers the required services in sequence.</summary>
    public virtual void Setup(IServiceCollection services)
    {
        ConfigureBaseServices(services);
        RegisterEntityFramework(services);
        RegisterHttpClients(services);
    }

    /// <summary>Step 1: Configure base services.</summary>
    protected virtual void ConfigureBaseServices(IServiceCollection services)
    {
        services.AddOptions();
    }

    /// <summary>Step 2: Register EF Core drivers using the options.</summary>
    protected virtual void RegisterEntityFramework(IServiceCollection services)
    {
        if (Options.ConfigureDb != null)
            services.AddDbContext<SummaryDbContext>(Options.ConfigureDb);
    }

    /// <summary>Step 3: Register any necessary HTTP clients.</summary>
    protected virtual void RegisterHttpClients(IServiceCollection services)
    {
        if (Options.ConfigureClient != null)
            services.AddHttpClient("validation", Options.ConfigureClient);
        else
            services.AddHttpClient("validation");
    }
}
