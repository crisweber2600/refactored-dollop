@ignore
Feature: Validation Rule Set
  Scenario: Multiple rules must pass for validation
    Given a clean db context
    When saving a new entity with rule set count 1 and sum 1
    Then the rule set entity should be validated
