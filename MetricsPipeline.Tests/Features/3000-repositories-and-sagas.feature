Feature: RepositoriesAndSagas
  Verify repository, unit of work and MassTransit DI setup.

  Scenario: Services are registered
    Then a repository should be provided
    And a unit of work should be provided
    And the bus should be provided

  Scenario: Persist and retrieve a summary record
    Given a new summary record with value 12.3
    When the record is added and saved
    And the record is retrieved
    Then the retrieved value should be 12.3
