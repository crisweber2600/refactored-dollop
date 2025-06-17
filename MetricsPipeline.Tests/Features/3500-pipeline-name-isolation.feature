Feature: PipelineNameIsolation
  Ensures validation uses history of the same pipeline name.

  Scenario: Distinct pipelines track separate histories
    Given the system is configured with a delta threshold of 5.0
    And pipeline "foo" has previously committed summary value 40.0
    And pipeline "bar" has previously committed summary value 70.0
    And the gather service returns:
      | MetricValue |
      | 44.5 |
      | 45.0 |
      | 45.5 |
    When pipeline "foo" is executed
    Then the delta from last run should be 5.0
    And the summary should be committed successfully
    When pipeline "bar" is executed
    Then the delta from last run should be 25.0
    And the summary should not be committed
