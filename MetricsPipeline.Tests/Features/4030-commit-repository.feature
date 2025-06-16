Feature: CommitRepository
  Validate async create, update and delete operations on the generic repository

  Scenario: Create returns key
    Given a summary record with value 5.0 for commit repo
    When the record is created via repository
    Then the created id should be greater than 0

  Scenario: Update returns key
    Given a summary record with value 3.0 for commit repo
    And the record is created via repository
    When the record value is updated to 6.0 via repository
    Then the updated id should equal the created id

  Scenario: Soft delete sets flag
    Given a summary record with value 2.0 for commit repo
    And the record is created via repository
    When the record is softly deleted via repository
    Then the record should be marked deleted

  Scenario: Hard delete blocked when not allowed
    Given a summary record with value 4.0 for commit repo
    And the record is created via repository
    When a hard delete is attempted via repository
    Then a HardDeleteNotPermittedException should be thrown

  Scenario: Hard delete allowed when repository configured
    Given a summary record with value 7.0 for commit repo with hard delete
    When a hard delete is attempted via repository
    Then the delete result id should equal the created id
    And retrieving the deleted record should return nothing
