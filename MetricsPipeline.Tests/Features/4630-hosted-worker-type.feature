Feature: HostedWorkerType
  Verify a custom worker type can be registered and executed

  Scenario: Execute worker provided by type
    When the pipeline is added with demo worker type
    And the demo worker runs
    Then the demo worker should return 3 items
