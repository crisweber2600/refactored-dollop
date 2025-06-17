namespace MetricsPipeline.Infrastructure;
using System;
using System.Net.Http;

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
    /// Registers <see cref="HttpMetricsClient"/>, <see cref="HttpGatherService"/>, and <see cref="HttpWorkerService"/> when true.
    /// </summary>
    public bool UseHttpWorker { get; set; }

    /// <summary>
    /// Registers <see cref="HttpMetricsClient"/> without altering the gather service.
    /// </summary>
    public bool RegisterHttpClient { get; set; }

    /// <summary>
    /// Optional configuration for the registered <see cref="HttpClient"/>.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureClient { get; set; }
}
