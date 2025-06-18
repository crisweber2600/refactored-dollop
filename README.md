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
