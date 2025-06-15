Feature: SummarizeMetricValues
  Aggregates raw metric values into a summary.

  Scenario: Compute average of numeric input values
    Given the input metric values are:
      | MetricValue |
      | 42.0 |
      | 43.0 |
      | 41.0 |
    When the system summarizes the values using "average"
    Then the result should be 42.0

  Scenario: Compute count of input values
    Given the input metric values are:
      | MetricValue |
      | 9.2 |
      | 8.7 |
    When the system summarizes the values using "count"
    Then the result should be 2

  Scenario: Compute sum of input values
    Given the input metric values are:
      | MetricValue |
      | 3.0 |
      | 7.0 |
      | 10.0 |
    When the system summarizes the values using "sum"
    Then the result should be 20.0

  Scenario: Handle summarization of empty input set
    Given there are no metric values to summarize
    When the system attempts to summarize using "average"
    Then the operation should fail with reason "NoData"

  Scenario Outline: Use different strategies for same data
    Given the input metric values are:
      | MetricValue |
      | 1.0 |
      | 2.0 |
      | 3.0 |
    When the system summarizes the values using "<Strategy>"
    Then the result should be <Expected>

    Examples:
      | Strategy | Expected |
      | average  | 2.0 |
      | count    | 3 |
      | sum      | 6.0 |
