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

  Scenario: Configure custom metric and threshold
    Given the validation flow configuration
      """
      { "Type": "ExampleData.YourEntity, ExampleData", "SaveValidation": true, "MetricProperty": "Id", "ThresholdType": "RawDifference", "ThresholdValue": 2 }
      """
    When the flows are applied
    Then a repository for YourEntity is available
    And a plan for YourEntity exists with threshold 2
    And the plan uses RawDifference threshold
