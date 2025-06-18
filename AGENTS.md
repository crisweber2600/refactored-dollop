# Agent Guidelines

## General Workflow
- Agents may run `dotnet` commands, including adding NuGet packages with `dotnet add package`.
- When introducing new functionality, first create BDD feature files and step definitions.
- Only add EF Core migrations when entity models change; avoid running `dotnet ef database update` automatically.
- Focus on implementing missing functionality and fixing tests. Keep tasks small.
- Ask for clarification whenever information is missing.

## Testing
- Run `dotnet test` on a clean clone to establish the baseline.
- Re-run `dotnet test` after modifications and commit only when tests succeed.
- Use `dotnet test --no-restore --no-build` to reduce build log noise.
- Treat warnings as errors and ensure coverage is above 80% by running `dotnet test --collect:"XPlat Code Coverage"`.

## Development Conventions
- Add missing packages with `dotnet add package` when needed.
- Place worker classes under `MetricsPipeline.Core/Infrastructure/Workers` for reuse across projects.
- Do not add `<Compile Remove="..." />` or `<Compile Include="..." />` elements when moving files; rely on the SDK's default globbing.
- Write new requirements in `.feature` files using Reqnroll and implement step definitions with dependency injection.
- Provide unit tests with mocks using Moq.

## Documentation
- Update `README.md` with improvements reflecting new functionality and guidance.
- Each pull request must include at least five distinct enhancements to the README.

## Lessons Learned
- Record lessons here to streamline future work (e.g., don't escape quotes in BDD step definition files).
- Running `dotnet test --collect:"XPlat Code Coverage"` produces coverage reports for compliance.
