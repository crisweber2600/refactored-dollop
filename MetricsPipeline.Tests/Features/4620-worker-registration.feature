Feature: WorkerRegistration
  Ensure the pipeline worker can be optionally registered

  Scenario: Worker not registered by default
    When the pipeline is added with default options
    Then the service provider should not contain PipelineWorker

  Scenario: Worker registered on request
    When the pipeline is added with worker enabled
    Then the service provider should contain PipelineWorker
