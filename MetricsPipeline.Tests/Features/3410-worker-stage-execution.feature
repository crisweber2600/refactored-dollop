Feature: WorkerStageExecution
  Verify the PipelineWorker interprets the orchestrator result.

  Scenario Outline: Worker handles <Outcome> result
    Given the worker is configured for <Outcome>
    When the worker runs
    Then the stage results should be "<Stages>"

    Examples:
      | Outcome | Stages    |
      | success | Committed |
      | failure | Reverted  |
