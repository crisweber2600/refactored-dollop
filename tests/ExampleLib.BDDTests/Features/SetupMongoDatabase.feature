Feature: SetupMongoDatabase Extension
  Scenario: Registers Mongo unit of work
    Given a new mongo service collection
    When SetupMongoDatabase is invoked
    Then a Mongo unit of work can be resolved
