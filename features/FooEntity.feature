Feature: Foo repository
  Scenario: Adding Foos
    Given no Foo records exist
    When a new Foo is added
    Then the Foo repository count should be 1
