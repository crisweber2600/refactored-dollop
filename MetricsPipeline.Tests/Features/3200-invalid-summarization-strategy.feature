Feature: InvalidSummarizationStrategy
  Ensure summarization fails for unsupported strategies.

  Scenario: Summarization with unknown strategy
    Given the input metric values are:
      | MetricValue |
      | 1.0 |
    When the system attempts to summarize using "invalid"
    Then the summarization should fail with reason "UnknownStrategy"
