Feature: SetupDatabase Extension
  Scenario: Registers DbContext and unit of work
    Given a new service collection
    When SetupDatabase is invoked
    Then a unit of work can be resolved
