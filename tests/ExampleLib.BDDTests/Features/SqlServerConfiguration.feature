@ignore
Feature: SQL Server configuration
  Scenario: DbContext is registered with SQL Server
    Given a new service collection
    When AddExampleDataSqlServer is invoked
    Then the DbContext should use SqlServer
