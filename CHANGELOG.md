# Changelog

All notable changes to this package are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **Breaking:** replaced the positional, `Span`-based calculator API with a handle-based
  declare/calculate API. `IPropertyCalculator` now exposes `Declare(CalculatorBuilder)`
  (called once at registration) and `Calculate(in CalculationContext)` (zero-alloc hot
  path), replacing `GetInputProperties(Span<PropertyFilter>)`, `GetOutputProperty()` and
  `Calculate(IReadOnlyList<...>, List<Property>)`. Calculators declare inputs via
  `builder.AddInput(...)` — which returns an `InputHandle` used to read that input in
  `Calculate` — and their output via `builder.SetOutput(...)`. Input order no longer has
  to match the reading code, and the previous 32-input cap is gone. New public types:
  `CalculatorBuilder`, `InputHandle`, `CalculationContext`, `InputGroup`, `OutputGroup`.

## [0.1.0] - 2026-07-16

### Added

- Initial release: reactive, dependency-graph-driven property/stat system.
- `PropertySystem<TEntity, TProperty>` supporting base/derived and global/instanced
  properties, with values exposed as R3 `ReadOnlyReactiveProperty<double>`.
- `IPropertyCalculator` for computing derived properties, with dirty propagation and
  topological evaluation driven by `Tick()`.
- Cycle detection at calculator registration time.
- EditMode test suite runnable in Unity and headless via `dotnet test`.
