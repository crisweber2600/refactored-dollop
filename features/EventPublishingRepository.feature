Feature: Event Publishing Repository
  Scenario: Saving publishes SaveRequested
    Given an event publishing repository
    When the entity is saved
    Then a SaveRequested event should be published
