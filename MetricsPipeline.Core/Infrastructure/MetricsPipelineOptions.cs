namespace MetricsPipeline.Infrastructure;
using System;
using System.Net.Http;
using MetricsPipeline.Core;

/// <summary>
/// Options controlling additional registrations when calling <see cref="DependencyInjection.AddMetricsPipeline"/>.
/// </summary>
public class MetricsPipelineOptions
{
    /// <summary>
    /// Registers <see cref="PipelineWorker"/> as a hosted service when true.
    /// </summary>
    public bool AddWorker { get; set; }

    /// <summary>
    /// Determines which gather and worker services are registered.
    /// Defaults to <see cref="WorkerMode.InMemory"/>.
    /// </summary>
    public WorkerMode WorkerMode { get; set; } = WorkerMode.InMemory;

    /// <summary>
    /// Registers <see cref="HttpMetricsClient"/> without altering the gather service.
    /// </summary>
    public bool RegisterHttpClient { get; set; }

    /// <summary>
    /// Optional type of the hosted worker registered when <see cref="AddWorker"/> is true.
    /// Defaults to <see cref="PipelineWorker"/>.
    /// </summary>
    public Type? WorkerType { get; set; }

    /// <summary>
    /// Optional configuration for the registered <see cref="HttpClient"/>.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureClient { get; set; }
}
