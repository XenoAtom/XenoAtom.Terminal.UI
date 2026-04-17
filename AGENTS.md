# XenoAtom.Terminal.UI - Agent Instructions

XenoAtom.Terminal.UI is a modern, reactive retained-mode terminal UI framework for .NET.

Paths/commands below are relative to this directory (repo root).

## Orientation

- Library: `src/XenoAtom.Terminal.UI/`
- Source generator: `src/XenoAtom.Terminal.UI.SourceGen/`
- Tests (MSTest): `src/XenoAtom.Terminal.UI.Tests/`
- Samples: `samples/`
- Docs/site: `site/` (user docs: `site/docs/`, specs: `site/docs/specs/`)
- Must-read dev docs: `site/docs/readme.md`, `site/docs/control-development.md`

## Build & Test

```sh
cd src
dotnet build -c Release
dotnet test -c Release
```

Website (only if touching `site/**`):

```sh
cd site
lunet build
```

Screenshots (only if control visuals changed):

```sh
dotnet run -c Release --project samples/ControlsDemo -- --export-screenshots
# outputs to site/img/controls
```

Use the generated PNG screenshots as a verification tool, not only as website assets:

- when a change affects rendering, export screenshots and inspect the relevant PNGs directly to confirm the UI looks correct
- agents may use the screenshot export path to visually validate spacing, clipping, emoji/icon rendering, command bars, and other pixel-level regressions
- if a rendering fix changes documented visuals, regenerate the affected screenshots before finishing

## Rules (Do/Don't)

- Keep diffs focused; avoid drive-by refactors/formatting and unnecessary dependencies.
- Follow existing patterns/naming; add comments only for non-obvious "why".
- New/changed behavior requires tests; bug fix = regression test first, then fix.
- Public APIs require XML docs (avoid CS1591) and should document thrown exceptions.
- If behavior changes, update docs: `readme.md` and `site/docs/**` (especially `site/docs/readme.md`).

## C# Defaults

- Naming: `PascalCase` public/types/namespaces, `camelCase` locals/params, `_camelCase` private fields, `I*` interfaces.
- Style: file-scoped namespaces; `using` outside namespace (`System` first); `var` when type is obvious.
- Nullability: enabled; respect annotations; prefer `is null`/`is not null`; no warning suppressions without a brief justification comment.
- Validation: use `ArgumentNullException.ThrowIfNull()`/`ArgumentException.ThrowIfNullOrEmpty()`; throw specific exceptions with helpful messages.
- Async: `Async` suffix; no `async void` (except event handlers); `ConfigureAwait(false)` in library code; consider `ValueTask<T>` on hot paths.

## Perf / AOT / Trimming

- Minimize allocations (`Span<T>`, `ArrayPool<T>`, `StringBuilder` in loops).
- Keep code AOT/trimmer-friendly: avoid reflection; prefer source generators; use `[DynamicallyAccessedMembers]` when reflection is required.
- Use `sealed` for non-inheritable classes.

## API Design

- Prefer overloads over optional params (binary compatibility); consider `Try*` alongside throwing APIs.
- Obsolete before removal once stable (`[Obsolete("...", error: false)]`).

## Related Repos

These repos are optional local checkouts and may not exist in the current workspace. If present, consult their `AGENTS.md`
for cross-repo changes; otherwise treat them as external dependencies (typically consumed via NuGet).

- `XenoAtom.Ansi`: `../XenoAtom.Ansi` (if checked out)
- `XenoAtom.Terminal`: `../XenoAtom.Terminal` (if checked out)

## Git / Pre-submit

- Commits: one logical change per commit unless the user asks for a single commit; imperative subject, < 72 chars.
- Pre-submit: `dotnet build -c Release` + `dotnet test -c Release`; `lunet build` when touching `site/**`; refresh screenshots when control visuals change.
- Do not delete unrelated local files.
