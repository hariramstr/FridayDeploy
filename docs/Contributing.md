# Contributing

Thanks for considering a contribution to FridayDeploy.

## Getting set up

```bash
dotnet restore
dotnet build FridayDeploy.slnx
dotnet run --project FridayDeploy.Web
```

Requires the .NET SDK version pinned in `global.json` (or matching the `<TargetFramework>` in the `.csproj` files if no `global.json` is present).

## Before opening a PR

- `dotnet build FridayDeploy.slnx` — the solution builds with zero warnings (`TreatWarningsAsErrors` is enabled in `FridayDeploy.Web`).
- `dotnet test` — all tests pass.
- New EF Core model changes need a migration: `dotnet ef migrations add <Name>` from `FridayDeploy.Web`.
- Keep changes scoped — prefer several small PRs over one large one.
- Match existing patterns: thin `[ApiController]` classes delegating to `Services/*`, MVC controllers + Razor views for the UI, `AsNoTracking()` for read-only EF Core queries.

## Reporting bugs / requesting features

Use the [issue templates](../.github/ISSUE_TEMPLATE) — they ask for the information needed to reproduce or evaluate the request.

## Code style

- Nullable reference types are enabled solution-wide — don't introduce `#nullable disable`.
- No comments explaining *what* code does (names should do that); comments are for non-obvious *why*.
- No premature abstraction — three similar lines beat a speculative interface used once.

## License

By contributing, you agree your contributions are licensed under the project's [MIT License](../LICENSE).
