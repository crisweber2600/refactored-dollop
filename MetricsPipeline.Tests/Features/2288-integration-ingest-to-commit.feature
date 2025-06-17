Feature: FullPipelineExecution
  Runs the full ingest -> summarize -> validate -> commit pipeline.

  Background:
    Given the system is configured with a delta threshold of 5.0
    And the previously committed summary value is 45.0

  Scenario: End-to-end success path with valid summary
    Given the gather service returns:
      | MetricValue |
      | 44.5 |
      | 45.0 |
      | 45.5 |
    When the pipeline is executed
    Then the summary should be 45.0
    And the delta from last run should be 0.0
    And the summary should be committed successfully

  Scenario: End-to-end failure due to invalid delta
    Given the gather service returns:
      | MetricValue |
      | 60.0 |
      | 61.0 |
      | 59.0 |
    When the pipeline is executed
    Then the summary should be 60.0
    And the delta from last run should be 15.0
    And the summary should not be committed
    And a "ValidationFailed" warning should be logged

  Scenario: End-to-end failure due to unavailable data
    Given the gather service returns no metric values
    When the pipeline is executed
    Then the operation should fail at the gather stage
    And the system should log an error with reason "NoData"
    And no summary should be computed or committed

  Scenario: End-to-end no-op for empty data set
    Given the gather service returns no metric values
    When the pipeline is executed
    Then the operation should halt at the summarize stage
    And the system should log an error with reason "NoData"
    And no validation or commit should occur
