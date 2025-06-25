Feature: AddValidatorService Extension
  Scenario: Manual validator can be resolved
    Given a new service collection
    When AddValidatorService is invoked
    And AddValidatorRule is added
    Then a manual validator can be resolved
