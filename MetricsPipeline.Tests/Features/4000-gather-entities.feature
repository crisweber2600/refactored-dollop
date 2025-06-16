Feature: GatherEntities
  Ensures SummaryDbContext automatically registers entity types.

  Scenario: Model registers entities from assembly
    When the database model is built
    Then the SummaryRecord entity should be mapped
