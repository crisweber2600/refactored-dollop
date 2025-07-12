Feature: AddValidationForEfCore Extension
  Scenario: Registers EF services and plan
    Given a new service collection
    When AddValidationForEfCore is invoked
    Then a repository and validator can be resolved
