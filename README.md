# RAGStart

This repository demonstrates a simple .NET setup with unit and BDD tests.

## Last Update
- Implemented EF Core repository pattern with DI tests on 2025-06-18.
- Added EF Core replication guide and documentation tests on 2025-06-19.
- Initialized solution and added basic projects on 2025-06-18.
- Renamed soft delete interface to IValidatable on 2025-06-18.
- Updated query filter to require Validated == true on 2025-06-20.
- Soft delete now sets Validated to false on 2025-06-20.
- Added validation strategies and SaveChanges validation on 2025-06-21.
- Clarified DeleteAsync docs for Validated flag on 2025-06-21.
- Added mock-based validation tests and DI step definitions on 2025-06-22.

- Verified all docs use the Validated property instead of IsDeleted on 2025-06-22.
- Introduced generic SaveRequested<T> and SaveValidated<T> events for persisting entities.
- Added SaveAudit records to track metric values and validation results.
- Created SummarisationPlan<T> for computing numeric metrics with threshold rules.
- Documented ThresholdType enum for raw difference versus percent change checks.
- Added examples explaining how these domain events support auditing workflows.
- Created IEntityRepository<T> to publish SaveRequested events when entities are saved.
- Introduced ISummarisationValidator<T> interface for encapsulating change validation logic.
- Added ISaveAuditRepository abstraction for storing SaveAudit records.
- Added ISummarisationPlanStore for retrieving plans by entity type.
- Documented how these interfaces improve testability of summarisation workflows.
- Implemented InMemorySummarisationPlanStore for registering plans in memory.
- Added InMemorySaveAuditRepository to track the latest audit per entity.
- Introduced Infrastructure namespace to organize testing helpers.
- Created unit tests verifying the new in-memory components.
- Explained thread-safe dictionaries to allow concurrent event handling.
- Implemented SummarisationValidator<T> to enforce metric thresholds.
- Added xUnit tests covering raw difference and percent change checks.
- Registered validator and repositories for BDD scenarios.
- Created BDD feature demonstrating summarisation validation rules.
- Documented how to configure plans and audit storage for tests.
- Implemented SaveValidationConsumer<T> using MassTransit to validate SaveRequested events.
- Added MassTransit packages and in-memory test harness for consumer tests.
- Created unit tests verifying audits and published events when a save occurs.
- Updated build instructions to restore new dependencies before running tests.
- Expanded documentation with steps to configure summarisation plans and audits for the consumer.
- Added EventPublishingRepository<T> to publish SaveRequested events on save.
- Implemented reflection-based ID detection with GUID fallback.
- Created xUnit tests covering event publication scenarios.
- Added BDD feature exercising the event publishing repository.
- Documented how to wire up MassTransit and use the new repository.
- Introduced ExampleRunner console app showing the complete validation workflow.
- Registered SaveValidationConsumer on an in-memory MassTransit bus.
- Demonstrated configuring a summarisation plan for an Order entity.
- Showed how EventPublishingRepository publishes SaveRequested events.
- Logged audit outcomes to verify SaveValidated results.
- Added instructions for running the example program to observe the flow.
- Introduced AddSaveValidation extension for simple DI registration on 2025-06-23.

## Using the Validation Workflow

1. Reference the **ExampleLib** project from your application.
2. Call `services.AddExampleDataSqlServer(connectionString)` to register `YourDbContext` with SQL Server and a unit of work implementation.
3. Call `services.AddSaveValidation<T>()` during startup to register the in-memory repositories, validator and MassTransit consumer.
   ```csharp
   services.AddExampleDataSqlServer("Server=(localdb)\\mssqllocaldb;Database=Orders;");
   services.AddSaveValidation<Order>(o => o.LineAmounts.Sum(), ThresholdType.PercentChange, 0.5m);
   ```
   The extension configures a default `SummarisationPlan` for `Order` entities. The metric selector, threshold type and value can be overridden.
4. Resolve `IEntityRepository<T>` and save entities as usual. The repository will automatically derive the application name using reflection:
   ```csharp
   var repo = provider.GetRequiredService<IEntityRepository<Order>>();
   await repo.SaveAsync(order);
   ```
5. Previous results are tracked in `ISaveAuditRepository` so each save is compared against the last audit.
6. To customize thresholds later, retrieve `ISummarisationPlanStore` and call `AddPlan()` with new values before saving.

See `src/ExampleRunner` for a runnable sample demonstrating these steps.

The projects include XML documentation and are structured so **ExampleLib** can be packed as a NuGet package when needed.
