Feature: GenericWorkerService
  Verify that the orchestrator can fetch typed items via IWorkerService

  Scenario: Summarise DTO values from the worker service
    Given the gather service returns:
      | Amount |
      | 4 |
      | 6 |
    When the generic pipeline is executed selecting Amount
    Then the summary should be 10
