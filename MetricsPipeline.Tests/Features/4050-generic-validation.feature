Feature: GenericValidation
  Validate lists of complex objects using property summarisation.

  Scenario: Validate sum of values within threshold
    Given the last committed summary value is 100
    And the configured maximum delta is 50
    And the following items exist:
      | Amount |
      | 20     |
      | 30     |
    When the list is validated by summing Amount
    Then the summary should be marked as valid

  Scenario: Validate average exceeds threshold
    Given the last committed summary value is 40
    And the configured maximum delta is 5
    And the following items exist:
      | Amount |
      | 60     |
      | 70     |
    When the list is validated by averaging Amount
    Then the summary should be marked as invalid
