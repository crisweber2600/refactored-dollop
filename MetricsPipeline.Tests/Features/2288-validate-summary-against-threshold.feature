Feature: ValidateSummaryAgainstThreshold
  Validates summary versus last baseline.

  Background:
    Given the last committed summary value is 50.0
    And the configured maximum delta is 5.0

  Scenario: Allow commit when summary is within delta threshold
    Given the current summary value is 53.5
    When the delta is calculated
    Then the delta should be 3.5
    And the summary should be marked as valid

  Scenario: Reject commit when summary exceeds threshold
    Given the current summary value is 57.2
    When the delta is calculated
    Then the delta should be 7.2
    And the summary should be marked as invalid

  Scenario: Allow commit when summary equals threshold exactly
    Given the current summary value is 55.0
    When the delta is calculated
    Then the delta should be 5.0
    And the summary should be marked as valid

  Scenario Outline: Validate various summary values
    Given the current summary value is <Current>
    When the delta is calculated
    Then the summary should be marked as <Outcome>

    Examples:
      | Current | Outcome  |
      | 49.0    | valid    |
      | 56.0    | invalid  |
      | 50.0    | valid    |
      | 44.5    | valid    |
      | 60.5    | invalid  |
