Feature: ExampleWorker
  Scenario: Sequential saves validate correctly
    Given an example worker
    When the initial values are saved
    Then the audit should be valid
    When values inside the threshold are saved
    Then the audit should still be valid
    When values outside the threshold are saved
    Then the audit should be invalid
