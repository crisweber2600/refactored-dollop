@ignore
Feature: Repository Update
  Scenario: Updating an entity persists changes
    Given a clean db context
    And an entity to update
    When the entity name is changed
    And the entity is updated
    And changes are committed
    Then the entity should reflect the new name
