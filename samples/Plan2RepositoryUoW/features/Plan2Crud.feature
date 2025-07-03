Feature: Plan2 CRUD Service
    Validate create scenarios

    Scenario: Create valid entity
        Given a plan2 service
        When I create an entity with score 80
        Then the entity should be validated

    Scenario: Create invalid entity
        Given a plan2 service
        When I create an entity with score 20
        Then the entity should be rejected
