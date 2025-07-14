Feature: Batch validation
  Validates batch insert sizes based on previous audits

  Scenario: First batch is always valid
    Given no previous batch audit
    When validating a batch of 10 entities
    Then the batch validation result should be true

  Scenario: Batch within 10 percent succeeds
    Given a previous batch size of 10
    When validating a batch of 11 entities
    Then the batch validation result should be true

  Scenario: Batch over 10 percent fails
    Given a previous batch size of 10
    When validating a batch of 12 entities
    Then the batch validation result should be false
