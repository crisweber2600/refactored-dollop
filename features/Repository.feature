Feature: Generic Repository
  Scenario: Add entity increases count
    Given a clean db context
    When a new entity is added
    Then the repository count should be 1
