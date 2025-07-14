@ignore
Feature: Summarisation Validator
  Scenario: First save with no prior audit is valid
    Given a summarisation plan using RawDifference threshold 5
    And no previous audit
    And the current metric is 10
    When validating the save
    Then the validation result should be true

  Scenario: Change within raw threshold is valid
    Given a summarisation plan using RawDifference threshold 5
    And a previous audit with metric 10
    And the current metric is 12
    When validating the save
    Then the validation result should be true

  Scenario: Percent change above threshold is invalid
    Given a summarisation plan using PercentChange threshold 0.1
    And a previous audit with metric 10
    And the current metric is 12
    When validating the save
    Then the validation result should be false
