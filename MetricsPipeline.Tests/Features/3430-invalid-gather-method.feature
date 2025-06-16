Feature: InvalidGatherMethodName
  Validate behaviour when an unknown gather method is specified.

  Scenario: Execute pipeline using unknown gather method
    Given the gather method is "NonExistentMethod"
    When the orchestrator executes with an invalid gather method
    Then the orchestrator should return an InvalidGatherMethod error
    And no summary should be computed or committed
