Feature: GenericRepository
  Validate generic repository with soft delete and specification filtering

  Scenario: Soft delete hides records
    Given a summary record with value 5 for generic repo
    When the generic record is added and saved
    Then the generic repository should return 1 active record
    When the generic record is deleted and saved
    Then the generic repository should return 0 active records

  Scenario: Specification filtering
    Given two summary records with values 5 and 15
    When the records are added and saved to generic repo
    And searching for records with value greater than 10
    Then the search result count should be 1
