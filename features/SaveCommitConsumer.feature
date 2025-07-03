Feature: Save Commit Consumer
  Scenario: Commit stores audit without faults
    Given a SaveCommit consumer
    When a valid SaveValidated message is processed
    Then a commit audit exists
    And no SaveCommitFault is published
