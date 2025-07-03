Feature: EF Core Replication Guide
  Ensures documentation is available

  Scenario: Guide mentions DbContext
    Given the EF Core replication guide is read
    Then it should contain "YourDbContext"

  Scenario: Guide mentions validation interface
    Given the EF Core replication guide is read
    Then it should contain "IValidatable"
