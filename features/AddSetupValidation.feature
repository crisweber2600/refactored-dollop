Feature: AddSetupValidation Extension
  Scenario: Builder configures and registers plan
    Given a new service collection
    When AddSetupValidation is invoked
    Then a repository and validator can be resolved
