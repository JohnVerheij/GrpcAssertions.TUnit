# GrpcAssertions.TUnit

[![CI](https://github.com/JohnVerheij/GrpcAssertions.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnVerheij/GrpcAssertions.TUnit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native gRPC assertions for .NET tests. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting on gRPC outcomes, with a framework-agnostic core (`GrpcAssertions`) that a future xUnit, NUnit, or MSTest adapter can reuse. AOT-compatible, trimmable, no runtime reflection in the assertion path.

## Status

> **v0.0.1 is a skeleton release.** It exists to establish the repository, claim the `GrpcAssertions` and `GrpcAssertions.TUnit` identifiers on nuget.org, and stand up the full family quality bar (AOT-clean, no runtime reflection, SBOM + SLSA provenance, public-API snapshot pinning). The surface is deliberately one assertion wide so it is real and exercised end-to-end rather than empty.
>
> **v0.1.0** brings the gRPC outcome-assertion surface: a `GrpcCallBuilder` test helper, the `ThrowsGrpcException` / `DoesNotThrowGrpcException` verbs with `WithDetail` / `WithDetailContaining` refinements, and `StatusCode` shorthands (`ThrowsNotFound()`, `ThrowsUnavailable()`, and the rest of the canonical set).

## Why

gRPC failures surface as a single `RpcException` carrying a `Status` (a `StatusCode` plus a detail string). Asserting on that with raw `try`/`catch` plus `Assert.That(ex.StatusCode).IsEqualTo(...)` is verbose and easy to get subtly wrong (forgetting to assert that an exception was thrown at all, or matching the wrong status). This package gives those checks a fluent, source-generated home that reads like the rest of your TUnit suite.

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

**Requirements:** TUnit 1.47.0 or later, .NET 10. The framework-agnostic `GrpcAssertions` core and its single `Grpc.Core.Api` dependency come transitively; install `GrpcAssertions` directly only when authoring a non-TUnit adapter.

## Package layout

| Package | Role | Depends on |
|---|---|---|
| [`GrpcAssertions`](https://www.nuget.org/packages/GrpcAssertions/) | Framework-agnostic core: predicates over gRPC exceptions and (from v0.1.0) the shared status-formatting logic. | `Grpc.Core.Api` |
| [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/) | TUnit adapter: the fluent `Assert.That(...)` entry points. **Most users want this one.** | `GrpcAssertions`, `TUnit.Assertions`, `TUnit.Core` |

The split keeps the assertion logic free of any single test framework, so the same core can back adapters for other frameworks without duplicating the gRPC-specific behaviour.

## Quick start

```csharp
[Test]
public async Task FailedCall_SurfacesRpcException()
{
    var caught = await Assert.That(() => client.GetAsync(request)).Throws<Exception>();

    await Assert.That(caught!).IsRpcException();
}
```

## Entry points

| Entry point | Receiver | Returns | Behaviour |
|---|---|---|---|
| `IsRpcException()` | `Exception` | `AssertionResult` | Asserts the exception is a gRPC `RpcException`. The failure message names the actual exception type. |

The framework-agnostic core exposes the same check as a plain predicate for non-TUnit consumers:

| Core API | Behaviour |
|---|---|
| `GrpcExceptions.IsRpcException(Exception?)` | `true` when the argument is a gRPC `RpcException`; `false` for `null` and for any other exception type. |

## Quality bar

This package ships at the same bar as the rest of the assertion family:

- **AOT-clean.** `IsAotCompatible` + `IsTrimmable`; no runtime reflection in the assertion path, enforced at build time by `BannedSymbols.txt`.
- **Public API pinned.** A snapshot test (`PublicApiTests`) fails on any change to either assembly's public surface until the baseline is explicitly re-accepted; ApiCompat strict-mode baseline validation is enabled from v0.1.0 (a first release has no baseline to compare against).
- **Supply chain.** Each release carries an SPDX 3.0 SBOM inside the nupkg, a CycloneDX 1.6 sibling SBOM, SLSA v1.0 build provenance, and Sigstore-signed attestations; commits and tags are SSH-signed; nuget.org publishing is OIDC Trusted Publishing. See [SECURITY.md](SECURITY.md) for the verification recipe.
- **CI gates.** Build / test / pack, CodeQL (`csharp` + `actions`), OpenSSF Scorecard, dependency-review, and a zizmor workflow-security audit.

## Family

`GrpcAssertions.TUnit` is part of a family of focused, TUnit-native assertion packages, each a framework-agnostic core plus a TUnit adapter:

| Package | Domain |
|---|---|
| `TimeAssertions.TUnit` | `TimeProvider`, timers, and scheduled-callback assertions |
| `MathAssertions.TUnit` | vectors, matrices, quaternions, poses, tolerance-aware comparisons |
| `JsonAssertions.TUnit` | JSON structure, JSONPath, canonicalization |
| `SnapshotAssertions.TUnit` | inline and file snapshot matching with scrubbers |
| `SseAssertions.TUnit` | Server-Sent Events wire-format and stream assertions |
| `LogAssertions.TUnit` | `ILogger` / log-record assertions |
| `GrpcAssertions.TUnit` | gRPC outcome assertions (this package) |

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for the build, test, and snapshot-acceptance workflow, and [CONVENTIONS.md](CONVENTIONS.md) for the family-wide structure and policy. By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

[MIT](LICENSE). Takes a single runtime dependency on `Grpc.Core.Api` (Apache-2.0).
