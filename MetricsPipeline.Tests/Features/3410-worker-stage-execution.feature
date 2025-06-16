Feature: WorkerStageExecution
  Verify the PipelineWorker executes the correct stages based on the orchestrator result.

  Scenario Outline: Worker handles <Outcome> result
    Given the worker is configured for <Outcome>
    When the worker runs
    Then the stage results should be "<Stages>"

    Examples:
      | Outcome | Stages                       |
      | success | Gathered,Validated,Committed |
      | failure | Gathered,Validated,Reverted  |
