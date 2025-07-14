@ignore
Feature: Sequence Validator with Plan
  Scenario: Differences within threshold are valid
    Given a summarisation plan using RawDifference threshold 5
    And a sequence "10,12,13" for server "a"
    When validating the sequence by server
    Then the validation result should be true

  Scenario: Excessive change fails validation
    Given a summarisation plan using RawDifference threshold 5
    And a sequence "10,20,12" for server "a"
    When validating the sequence by server
    Then the validation result should be false
