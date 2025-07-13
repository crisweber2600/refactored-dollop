Feature: Mongo Bulk Inserts
  Scenario: Bulk insert increases count
    Given a clean mongo database
    When two mongo entities are bulk inserted
    Then the mongo repository count should be 2
