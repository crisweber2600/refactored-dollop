Feature: UnitOfWorkCommit
  Ensure repository changes persist when SaveChanges is invoked.

  Scenario: Persist new record through Unit of Work
    Given a new summary record with value 55.5
    When the record is added and saved
    And the record is retrieved
    Then the retrieved value should be 55.5

  Scenario: Removing a record and committing
    Given a new summary record with value 10.0
    When the record is added and saved
    And the record is retrieved
    Then the retrieved value should be 10.0
    When the record is deleted and saved
    And the record is retrieved
    Then no record should be found
