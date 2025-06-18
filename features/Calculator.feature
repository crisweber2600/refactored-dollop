Feature: Calculator
  To verify addition
  Scenario: Add two numbers
    Given I have two numbers 2 and 3
    When they are added
    Then the result should be 5
