Feature: RevertInMemoryDatabase
  Ensures the in-memory provider can be initialized without migrations.

  Scenario: Initialize in-memory database
    Given a new in-memory SummaryDbContext
    When the context is inspected
    Then migrations should not run
    And the database is empty
