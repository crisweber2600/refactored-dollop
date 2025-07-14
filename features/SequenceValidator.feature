Feature: SequenceValidator
  Validates sequences using dynamic property selectors

  Scenario: Validation passes
    Given a foo sequence that should pass
    When validating with a delta rule of 2
    Then the sequence validation result should be true

  Scenario: Validation fails
    Given a foo sequence that should fail
    When validating with a delta rule of 5
    Then the sequence validation result should be false
