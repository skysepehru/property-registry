# Changelog

All notable changes to this package are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-16

### Added

- Initial release: reactive, dependency-graph-driven property/stat system.
- `PropertySystem<TEntity, TProperty>` supporting base/derived and global/instanced
  properties, with values exposed as R3 `ReadOnlyReactiveProperty<double>`.
- `IPropertyCalculator` for computing derived properties, with dirty propagation and
  topological evaluation driven by `Tick()`.
- Cycle detection at calculator registration time.
- EditMode test suite runnable in Unity and headless via `dotnet test`.
