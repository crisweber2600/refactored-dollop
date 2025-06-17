Feature: ServiceModeSelection
  Verify AddMetricsPipeline registers services based on the selected mode.

  Scenario: Use HTTP mode
    When the service provider is built with mode Http
    Then IGatherService should resolve to HttpGatherService
    And IWorkerService should resolve to HttpWorkerService

  Scenario: Default InMemory mode
    When the service provider is built with default mode
    Then IGatherService should resolve to InMemoryGatherService
    And IWorkerService should resolve to InMemoryGatherService
