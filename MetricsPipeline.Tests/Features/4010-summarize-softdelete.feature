Feature: SoftDeleteFilter
  Validates global soft delete filtering and repository opt-out

  Background:
    Given a soft deleted summary record exists

  Scenario: Deleted summaries excluded by default
    When counting all summaries
    Then the summary count should be 0

  Scenario: Deleted summaries included when filter ignored
    Given the repository ignores the soft delete filter
    When counting all summaries
    Then the summary count should be 1

  Scenario: Filter does not affect entities without ISoftDelete
    Given a simple record exists
    When counting simple records
    Then the simple record count should be 1

  @EdgeCase
  Scenario: Deleted entity looked up by id returns null
    When finding deleted summary by id
    Then the result should be null
