Feature: MassTransit Reliability
  Scenario: Messages are retried and logged
    Given a reliability configured service collection
    When a failing message is published
    Then the consumer should retry
