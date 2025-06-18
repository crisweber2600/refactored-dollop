Feature: Unit of Work Validation
  Scenario: Saving sets validation based on count strategy
    Given a clean db context
    When saving a new entity with count threshold 1
    Then the entity should be validated
