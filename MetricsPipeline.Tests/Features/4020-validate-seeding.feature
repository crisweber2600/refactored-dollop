Feature: ValidateSeeding
  Seeds the database from JSON files.

  Scenario: Insert records from JSON
    Given a seed file "seed.json" containing:
      """
      [
        { "PipelineName": "demo", "Source": "https://ex.com", "Value": 5, "Timestamp": "2024-01-01T00:00:00Z" }
      ]
      """
    When the seeding service is executed
    Then the repository should contain 1 record

  Scenario: Skip duplicate records
    Given a seed file "seed.json" containing:
      """
      [
        { "PipelineName": "demo", "Source": "https://ex.com", "Value": 5, "Timestamp": "2024-01-01T00:00:00Z" }
      ]
      """
    And the seeding service is executed
    When the seeding service is executed
    Then the repository should contain 1 record

  Scenario: Malformed JSON raises error
    Given a seed file "bad.json" containing:
      """
      [ { "PipelineName": "demo", }
      """
    When executing the seeding service
    Then a SeedValidationException should be thrown for "bad.json" line 0
