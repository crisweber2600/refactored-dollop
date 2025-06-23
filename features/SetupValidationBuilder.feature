Feature: SetupValidationBuilder
  Scenario: SQL Server configuration is applied
    Given a new setup builder
    When UseSqlServer is called
    And Apply is invoked
    Then a unit of work can be resolved

  Scenario: Mongo configuration is applied
    Given a new setup builder
    When UseMongo is called
    And Apply is invoked
    Then a Mongo unit of work can be resolved
