@ignore
Feature: ManualValidatorService
  Validates objects using explicit rules

  Scenario: No rules registered
    Given a manual validator with no rules
    When I validate the instance
    Then the manual validation result should be true

  Scenario: Rule passes
    Given a manual validator with a rule that returns true
    When I validate the instance
    Then the manual validation result should be true

  Scenario: Rule fails
    Given a manual validator with a rule that returns false
    When I validate the instance
    Then the manual validation result should be false
