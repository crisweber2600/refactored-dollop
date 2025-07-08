Feature: AddSetupValidation Extension
  Scenario: Builder configures and registers plan
    Given a new service collection
    When AddSetupValidation is invoked
    Then a repository and validator can be resolved

  Scenario: Mongo builder registers mongo repository
    Given a new service collection
    When AddSetupValidation is invoked with Mongo
    Then a mongo repository and validator can be resolved
