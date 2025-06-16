Feature: TaskExecutionStrategy
  Verify correct strategy selection for pipeline stages.

  Scenario Outline: Execute <Stage> stage
    Given a task stage "<Stage>"
    When the task executor runs
    Then the result message should be "<Message>"

    Examples:
      | Stage    | Message   |
      | Gather   | Gathered  |
      | Validate | Validated |
      | Commit   | Committed |
      | Revert   | Reverted  |
