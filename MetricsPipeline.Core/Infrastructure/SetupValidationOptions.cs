namespace MetricsPipeline.Infrastructure;

using System;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Options controlling service registration for the setup validation phase.
/// </summary>
public class SetupValidationOptions
{
    /// <summary>
    /// Optional database configuration delegate used when registering the context.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDb { get; set; }

    /// <summary>
    /// Optional configuration applied to the created <see cref="HttpClient"/>.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureClient { get; set; }
}
