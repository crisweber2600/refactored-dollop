@ignore
Feature: Mongo configuration
  Scenario: Mongo database is registered
    Given a new service collection
    When AddExampleDataMongo is invoked
    Then the Mongo database should resolve
