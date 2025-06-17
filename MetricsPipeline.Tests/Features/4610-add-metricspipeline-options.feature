Feature: MetricsPipelineOptions
  Validate optional service registrations when adding the metrics pipeline

  Scenario: Register pipeline worker hosted service
    When the pipeline is added with hosted worker
    Then the service provider should contain PipelineWorker

  Scenario: Register HttpMetricsClient only
    When the pipeline is added with HttpClient
    Then the service provider should contain HttpMetricsClient

  Scenario: Set worker mode to Http
    When the pipeline is added with HTTP worker
    Then IGatherService should be HttpGatherService

  Scenario: Default worker mode is InMemory
    When the pipeline is added with default worker mode
    Then IGatherService should be InMemoryGatherService
