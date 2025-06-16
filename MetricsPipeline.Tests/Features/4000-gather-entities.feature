Feature: GatherEntities
  Ensures SummaryDbContext automatically registers entity types.

  Scenario: Model registers entities from assembly
    When the database model is built
    Then the SummaryRecord entity should be mapped
    And the ExtraRecord entity should be mapped

  Scenario: Configuration classes applied automatically
    When the database model is built
    Then the SummaryRecord PipelineName max length should be 50

  Scenario: New entity appears without DbContext edits
    When the database model is built
    Then the ExtraRecord entity should be mapped
    And Set<ExtraRecord> should be available

  @EdgeCase
  Scenario: Entity without configuration loads with conventions
    When the database model is built
    Then Set<SimpleRecord> should be available
