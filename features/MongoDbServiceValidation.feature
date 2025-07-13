Feature: MongoDbService Validation
  Scenario: Decorated service validates inserted documents
    Given a validating mongo service
    When items are inserted via the service
    Then the nanny collection count should be 1
