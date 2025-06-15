- Agents may run `dotnet` commands, including adding NuGet packages with `dotnet add package`.
- When introducing new functionality, first add BDD feature files and step definitions to cover the behavior.
- Always run `dotnet test` and ensure tests pass after modifications.
- Focus development efforts on implementing missing functionality and fixing tests.
- If required information is missing, request clarification.

- Run `dotnet test` on a clean clone to establish the baseline.
- Make code changes and add missing packages with `dotnet add package` when needed.
- Re-run `dotnet test`; only commit when all tests succeed.
- Keep tasks small and focused on making the tests pass.
