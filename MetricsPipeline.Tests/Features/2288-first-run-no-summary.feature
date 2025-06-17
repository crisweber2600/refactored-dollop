Feature: FirstRunNoSummary
  Runs pipeline when no prior summary exists.

  Scenario: Run pipeline when no summary exists
    Given the system is configured with a delta threshold of 50.0
    And the gather service returns:
      | MetricValue |
      | 44.5 |
      | 45.0 |
      | 45.5 |
    When the pipeline is executed
    Then the summary should be 45.0
    And the delta from last run should be 45.0
    And the summary should be committed successfully
