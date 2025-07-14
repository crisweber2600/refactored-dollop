@ignore
Feature: Validation Plan Factory
  Scenario: Generates default plans for each property
    When creating validation plans for a composite type
    Then 3 validation plans should be created
    And each plan should use Count strategy
