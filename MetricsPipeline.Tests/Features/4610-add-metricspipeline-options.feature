Feature: MetricsPipelineOptions
  Validate optional service registrations when adding the metrics pipeline

  Scenario: Register pipeline worker hosted service
    When the pipeline is added with hosted worker
    Then the service provider should contain PipelineWorker

  Scenario: Register HttpMetricsClient only
    When the pipeline is added with HttpClient
    Then the service provider should contain HttpMetricsClient

  Scenario: Register HTTP worker services
    When the pipeline is added with HTTP worker
    Then IGatherService should be HttpGatherService
