@ignore
Feature: Validation Filter
  Scenario: Unvalidated entities are excluded
    Given a context with an unvalidated entity
    When querying for all entities
    Then the result list should be empty
