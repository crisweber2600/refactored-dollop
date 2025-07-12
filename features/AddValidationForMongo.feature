Feature: AddValidationForMongo Extension
  Scenario: Registers Mongo services and plan
    Given a new service collection
    When AddValidationForMongo is invoked
    Then a mongo repository and validator can be resolved
