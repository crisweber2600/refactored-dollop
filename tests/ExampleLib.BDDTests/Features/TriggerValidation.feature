@ignore
Feature: Save Triggers Validation
  Scenario: Saving an entity triggers validation
    Given a clean db context
    When saving a new entity with count threshold 1
    Then the validation summary should be 1
