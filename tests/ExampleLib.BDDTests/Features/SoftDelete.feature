@ignore
Feature: Soft Delete
  Scenario: Soft deleting an entity marks it unvalidated
    Given a clean db context
    And an entity to delete
    When the entity is deleted
    Then the entity should be marked unvalidated
