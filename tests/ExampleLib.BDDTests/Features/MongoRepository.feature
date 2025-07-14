@ignore
Feature: Mongo Generic Repository
  Scenario: Add entity increases count
    Given a clean mongo database
    When a new mongo entity is added
    Then the mongo repository count should be 1
