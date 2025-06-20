- Agents may run `dotnet` commands, including adding NuGet packages with `dotnet add package`.
- When introducing new functionality, first add BDD feature files and step definitions to cover the behavior.
- Only add new EF Core migrations when entity models change. Do not run `dotnet ef database update` automatically.
- Always run `dotnet test` and ensure tests pass after modifications.
- Focus development efforts on implementing missing functionality and fixing tests.
- If required information is missing, request clarification.

- Run `dotnet test` on a clean clone to establish the baseline.
- Make code changes and add missing packages with `dotnet add package` when needed.
- Re-run `dotnet test`; only commit when all tests succeed.
- Keep tasks small and focused on making the tests pass.
- Update `README.md` with improvements reflecting new functionality and guidance.
- Each pull request must contain at least five distinct enhancements to the README.
- After each run, refine this file with lessons learned to streamline future work.
- Running `dotnet test --no-restore --no-build` avoids lengthy restore steps and
  reduces build log noise.
- Place worker classes under `MetricsPipeline.Core/Infrastructure/Workers` for reuse across projects.
- Do not add `<Compile Remove="..." />` or `<Compile Include="..." />` elements when moving files; rely on the SDK's default globbing.
