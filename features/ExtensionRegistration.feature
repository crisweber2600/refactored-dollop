Feature: AddSaveValidation Extension
  Scenario: Services are registered with defaults
    Given a new service collection
    When AddSaveValidation is invoked
    Then a repository and validator can be resolved
