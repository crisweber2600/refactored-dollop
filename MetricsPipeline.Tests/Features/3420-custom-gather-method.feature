Feature: CustomGatherMethod
  Verify the orchestrator can use a specified gather method.

  Scenario: Execute pipeline using alternate gather method
    Given the gather method is "CustomGatherAsync"
    And the API at "https://api.example.com/data" returns:
      | MetricValue |
      | 44.5 |
      | 45.0 |
      | 45.5 |
    When the pipeline is executed
    Then the summary should be 45.0
    And the summary should be committed successfully
