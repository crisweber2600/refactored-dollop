Feature: SoftDeleteFilter
  Validates global soft delete filtering and repository opt-out

  Scenario: Deleted summaries excluded by default
    Given a soft deleted summary record exists
    When counting all summaries
    Then the summary count should be 0

  Scenario: Deleted summaries included when filter ignored
    Given a soft deleted summary record exists
    And the repository ignores the soft delete filter
    When counting all summaries
    Then the summary count should be 1
