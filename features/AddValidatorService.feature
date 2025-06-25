Feature: AddValidatorService Extension
  Scenario: Services are registered
    Given a new service collection
    When AddValidatorService is invoked
    Then the manual validator can be resolved

  Scenario: Rules can be registered
    Given a new service collection
    When AddValidatorService is invoked
    And AddValidatorRule is invoked
    Then the manual validator validates successfully
