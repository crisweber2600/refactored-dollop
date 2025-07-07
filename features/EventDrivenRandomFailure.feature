Feature: EventDriven Order Processing
  Demonstrates event-driven validation with occasional failures

  Scenario: Orders are audited
    When the demo runs with 5 orders
    Then both valid and invalid audits should exist

  Scenario: Commit consumer persists only valid orders
    Given commit consumer is enabled
    When the demo runs with 5 orders
    Then only valid orders should be stored
