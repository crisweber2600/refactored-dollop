Feature: CommitOrDiscardSummary
  Commits or discards summary based on validation result.

  Background:
    Given the system has a summarized value of 47.0

  Scenario: Commit valid summary to persistent storage
    Given the summary is marked as valid
    When the system executes commit
    Then the summary should be saved in the database
    And the commit timestamp should be recorded

  Scenario: Discard summary if marked invalid
    Given the summary is marked as invalid
    When the system executes commit
    Then the summary should not be saved
    And a warning should be logged with reason "ValidationFailed"

  Scenario: Handle failure during commit operation
    Given the summary is valid
    And the database is temporarily unavailable
    When the system attempts to commit
    Then the commit should fail with reason "DatabaseError"
    And the summary should remain uncommitted

  Scenario Outline: Outcome based on validation state
    Given the summary is marked as <State>
    When the system attempts to commit
    Then the result should be <Outcome>

    Examples:
      | State   | Outcome       |
      | valid   | committed     |
      | invalid | discarded     |
      | null    | error:unknown |
