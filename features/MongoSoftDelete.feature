Feature: Mongo Soft Delete
  Scenario: Soft deleting an entity marks it unvalidated
    Given a clean mongo database
    And a mongo entity to delete
    When the mongo entity is deleted
    Then the mongo entity should be marked unvalidated
