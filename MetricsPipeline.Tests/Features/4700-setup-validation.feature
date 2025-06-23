Feature: SetupValidation
  Verify setup validation extension methods register required services

  Scenario: SetupValidation registers context and client
    When the setup validation is executed
    Then the service provider should resolve SummaryDbContext
    And the service provider should resolve HttpClient

  Scenario: AddSetupValidation resolves typed validator
    When a typed validator is registered
    Then the typed validator should resolve
