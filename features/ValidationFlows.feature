Feature: ValidationFlows configuration
  Scenario: Configure from JSON array
    Given the validation flow configuration
      """
      [
        { "Type": "ExampleData.YourEntity, ExampleData", "SaveValidation": true }
      ]
      """
    When the flows are applied
    Then a repository for YourEntity is available

  Scenario: Configure from JSON object
    Given the validation flow configuration
      """
      { "Type": "ExampleData.YourEntity, ExampleData", "SaveValidation": true }
      """
    When the flows are applied
    Then a repository for YourEntity is available
