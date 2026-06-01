# GrpcAssertions.TUnit

[![CI](https://github.com/JohnVerheij/GrpcAssertions.TUnit/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnVerheij/GrpcAssertions.TUnit/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![Downloads](https://img.shields.io/nuget/dt/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native gRPC assertions for .NET tests. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting on gRPC call outcomes, with a framework-agnostic core (`GrpcAssertions`) that a future xUnit, NUnit, or MSTest adapter can reuse. AOT-compatible, trimmable, no runtime reflection in the assertion path.

## Table of contents

- [Why this package](#why-this-package)
- [Install](#install)
- [Package layout](#package-layout)
- [Namespaces (and a `GlobalUsings.cs` recommendation)](#namespaces-and-a-globalusingscs-recommendation)
- [Quick start](#quick-start)
- [Entry points](#entry-points)
- [Failure diagnostics](#failure-diagnostics)
- [Cookbook: common patterns](#cookbook-common-patterns)
- [Design notes](#design-notes)
- [Stability intent (pre-1.0)](#stability-intent-pre-10)
- [Roadmap](#roadmap)
- [Family compatibility](#family-compatibility)
- [Pair with](#pair-with)
- [Contributing](#contributing)
- [License](#license)

---

## Why this package

gRPC failures surface as a single `RpcException` carrying a `Status` (a `StatusCode` plus a detail string). Asserting on that with raw `try`/`catch` plus `Assert.That(ex.StatusCode).IsEqualTo(...)` is verbose and easy to get subtly wrong: forgetting to assert that an exception was thrown at all, or matching the wrong status. Typical hand-rolled code:

```csharp
var ex = await Assert.That(() => client.PredictAsync(request)).Throws<RpcException>();
await Assert.That(ex!.StatusCode).IsEqualTo(StatusCode.Unavailable);
await Assert.That(ex.Status.Detail).Contains("connection refused");
```

This library collapses that to one chain, and ships the `GrpcCallBuilder` test-double helper that removes the five-parameter `AsyncUnaryCall<T>` constructor every hand-rolled gRPC fake repeats.

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

**Requirements:** TUnit 1.47.0 or later, .NET 10. The framework-agnostic `GrpcAssertions` core and its single `Grpc.Core.Api` dependency come transitively. The package is AOT-compatible, trimmable, and uses no runtime reflection in the assertion path.

## Package layout

This repo ships **two** NuGet packages:

| Package | Purpose | Depends on |
|---|---|---|
| [`GrpcAssertions`](https://www.nuget.org/packages/GrpcAssertions/) | Framework-agnostic core: `GrpcCallBuilder` test-double builders, `GrpcOutcomeRendering` failure-message formatting, and `GrpcExceptions` predicates | `Grpc.Core.Api` |
| [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/) | TUnit `Assert.That(...)` entry points: `ThrowsGrpcException`, the `StatusCode` shorthands, detail refinements, and `DoesNotThrowGrpcException`. **Most users want this one.** | `GrpcAssertions` + `TUnit.Assertions` + `TUnit.Core` |

You install `GrpcAssertions.TUnit`; `GrpcAssertions` and `Grpc.Core.Api` come transitively. Adapters for other test frameworks (NUnit, xUnit, MSTest) are *not* shipped today: they would reuse the `GrpcAssertions` core. Open a feature request if you need one.

## Namespaces (and a `GlobalUsings.cs` recommendation)

The two packages place types in namespaces with deliberately-different scopes:

| Type / member | Namespace | Auto-imported? |
|---|---|---|
| `ThrowsGrpcException()`, `DoesNotThrowGrpcException()`, `IsRpcException()` and the `IsUnavailable()` / `WithDetail()` chain | `TUnit.Assertions.Extensions` | **Yes**: TUnit auto-imports |
| `GrpcExceptionAssertion`, `GrpcDoesNotThrowAssertion<T>` (the assertion classes behind the chain) | `GrpcAssertions.TUnit` | **No**: rarely referenced directly |
| `GrpcCallBuilder`, `GrpcExceptions`, `GrpcOutcomeRendering` (test-double builder, predicates, rendering) | `GrpcAssertions` | **No**: needed at the call site; recommended for `GlobalUsings.cs` |
| `RpcException`, `StatusCode`, `Status`, `Metadata` (the gRPC types) | `Grpc.Core` | **No**: needed at the call site; recommended for `GlobalUsings.cs` |

**Recommended:** put the two non-auto-imported namespaces into a single `GlobalUsings.cs` in your test project so every test file sees them without ceremony:

```csharp
global using Grpc.Core;        // StatusCode, RpcException, Status, Metadata
global using GrpcAssertions;   // GrpcCallBuilder, GrpcExceptions, GrpcOutcomeRendering
```

## Quick start

```csharp
// Assert a call faults with a specific status and detail, in one chain:
await Assert.That(() => client.PredictAsync(request, ct))
    .ThrowsGrpcException(StatusCode.Unavailable)
    .WithDetailContaining("connection refused", StringComparison.Ordinal);

// Status shorthands read fluently:
await Assert.That(() => client.GetServerInfoAsync(request, ct))
    .ThrowsGrpcException()
    .IsUnimplemented();

// Assert a benign error is swallowed and the call completes:
await Assert.That(() => client.ClosePickCycleAsync(request, ct))
    .DoesNotThrowGrpcException();

// Build AsyncUnaryCall<T> test doubles without the five-parameter constructor:
var ok = GrpcCallBuilder.Success(new PredictReply());
var bad = GrpcCallBuilder.Faulted<PredictReply>(StatusCode.NotFound, "no such cycle");
```

## Entry points

Delegate assertions, on `Assert.That(() => client.Method(...))` (auto-imported from `TUnit.Assertions.Extensions`):

| Entry point | Behaviour |
|---|---|
| `ThrowsGrpcException()` | Asserts the call throws a gRPC `RpcException` of any status. Returns a chain. |
| `ThrowsGrpcException(StatusCode expected)` | Asserts the call throws an `RpcException` with the given status. Returns a chain. |
| `DoesNotThrowGrpcException()` | Asserts the call completes without throwing an `RpcException`. |

Chain off `ThrowsGrpcException()` to refine:

| Chain method | Behaviour |
|---|---|
| `IsOk()`, `IsCancelled()`, `IsInvalidArgument()`, `IsDeadlineExceeded()`, `IsNotFound()`, `IsAlreadyExists()`, `IsPermissionDenied()`, `IsResourceExhausted()`, `IsFailedPrecondition()`, `IsAborted()`, `IsUnimplemented()`, `IsInternal()`, `IsUnavailable()`, `IsUnauthenticated()` | Assert the status equals the corresponding `StatusCode`. |
| `WithDetail(string)` | Assert `Status.Detail` exactly equals the string (ordinal). |
| `WithDetailContaining(string, StringComparison)` | Assert `Status.Detail` contains the substring using the given comparison. |

Exception discriminator, on a caught `Exception`:

| Entry point | Behaviour |
|---|---|
| `IsRpcException()` | Asserts the exception is a gRPC `RpcException`. The failure message names the actual exception type. |

Framework-agnostic core (`GrpcAssertions` namespace), for test doubles and non-TUnit consumers:

| Core API | Behaviour |
|---|---|
| `GrpcCallBuilder.Success<T>(T response)` | Builds a successful `AsyncUnaryCall<T>` (response, empty trailers, terminal `OK`). |
| `GrpcCallBuilder.Faulted<T>(RpcException)` / `Faulted<T>(StatusCode, string?)` | Builds a faulted `AsyncUnaryCall<T>` surfacing the exception's status and trailers. |
| `GrpcExceptions.IsRpcException(Exception?)` | `true` when the argument is a gRPC `RpcException`; `false` for `null` or any other type. |
| `GrpcOutcomeRendering.Describe(RpcException)` | Renders `RpcException with StatusCode X, Detail "..."` for failure messages. |

## Failure diagnostics

Every failed assertion renders the actual gRPC outcome alongside the expectation. A status mismatch:

```
Expected the gRPC call to throw an RpcException with StatusCode Unavailable
but it threw RpcException with StatusCode Internal, Detail "Unhandled exception in pipeline"
```

A call that should have thrown but completed:

```
Expected the gRPC call to throw an RpcException
but no exception was thrown
```

A `DoesNotThrowGrpcException()` that faulted:

```
Expected the gRPC call not to throw an RpcException
but it threw RpcException with StatusCode Unavailable, Detail "connection refused"
```

`Status.Detail` is truncated at `GrpcOutcomeRendering.MaxDetailLength` (200 characters) with a horizontal-ellipsis suffix, so a verbose server detail does not flood the test output.

## Cookbook: common patterns

**Assert a specific failure, status and detail in one chain:**

```csharp
await Assert.That(() => client.PredictAsync(request, ct))
    .ThrowsGrpcException(StatusCode.InvalidArgument)
    .WithDetailContaining("field 'id' is required", StringComparison.Ordinal);
```

**Assert any gRPC failure without pinning the status** (when the status is environment-dependent):

```csharp
await Assert.That(() => client.PredictAsync(request, ct)).ThrowsGrpcException();
```

**Assert a benign-error swallow** (the client catches a known `RpcException` and completes):

```csharp
await Assert.That(() => client.ClosePickCycleAsync(request, ct)).DoesNotThrowGrpcException();
```

**Remove the five-parameter constructor from a gRPC client fake:**

```csharp
// before: new AsyncUnaryCall<PredictReply>(Task.FromResult(reply), Task.FromResult(new Metadata()),
//             () => new Status(StatusCode.OK, ""), () => new Metadata(), () => { });
// after:
public override AsyncUnaryCall<PredictReply> PredictAsync(PredictRequest request, CallOptions options)
    => _fail ? GrpcCallBuilder.Faulted<PredictReply>(StatusCode.Unavailable, "server down")
             : GrpcCallBuilder.Success(_reply);
```

**Discriminate a caught exception before inspecting it:**

```csharp
var caught = await Assert.That(() => client.PredictAsync(request, ct)).Throws<Exception>();
await Assert.That(caught!).IsRpcException();
```

## Design notes

- **Assertions on delegates, not on `RpcException` instances.** The primary entry point is `Assert.That(() => client.Method(...))`. The library executes the call and inspects the thrown `RpcException`, which is cleaner than requiring the test to catch the exception itself. Internally the caught exception is moved into the assertion value via TUnit's `MapException<RpcException>()`: an `RpcException` becomes the value, any other thrown exception leaves the value null with the metadata exception populated, and nothing thrown leaves both null.
- **`Grpc.Core.Api` only.** The package depends on `Grpc.Core.Api` (the minimal API surface containing `RpcException`, `StatusCode`, `Status`, `Metadata`), not `Grpc.Net.Client` or `Google.Protobuf`, so it works with any gRPC implementation. The dependency is intrinsic: the public surface is typed against the consumer's real `RpcException`, so a home-grown enum would not compile against thrown exceptions.
- **No Protobuf dependency.** The library asserts on gRPC transport-level outcomes (status, detail), not on Protobuf message structure. Assert on response message fields with standard TUnit assertions on the deserialized response object.
- **Explicit `StringComparison` on detail assertions.** `WithDetailContaining` requires a `StringComparison`, matching the convention across the assertion family. `WithDetail` is exact and ordinal.
- **`GrpcCallBuilder` is test infrastructure, not an assertion.** It builds `AsyncUnaryCall<T>` instances for fakes and lives in the framework-agnostic core, so a future non-TUnit adapter reuses it. Both builders guard their arguments with `ArgumentNullException.ThrowIfNull`, a strict improvement over hand-rolled fakes that dereference null later.
- **No runtime reflection** in the assertion path; AOT-clean and trimmable.

## Stability intent (pre-1.0)

Every release through 1.0 is **additive**. The public API of both assemblies is pinned by a snapshot test (`PublicApiTests`) that fails on any change to a public type, member, signature, attribute, or visibility, and `EnablePackageValidation` strict-mode ApiCompat validates each release against its previous baseline at pack time. New surface is added; existing surface is not reshaped within a 0.x line. The 1.0.0 release locks the SemVer contract.

## Roadmap

Scoped to what real consumer suites use; later minor releases add surface as demand appears. All additive.

- **0.2.0**: trailer assertions (`HasTrailer`, `DoesNotHaveTrailer`, `HasTrailerCount`), response-header metadata assertions, `WithoutDetail()`, and `IsNotStatusCode(StatusCode)`.
- **0.3.0**: server-streaming assertions (`StreamsAtLeast`, `StreamsExactly`, `StreamContains`, `AndStreamItems<T>`).
- **0.4.0**: deadline and cancellation assertions (`ThrowsDeadlineExceeded`, `ThrowsCancelled`, `CompletesWithin(TimeSpan, TimeProvider)` per the cross-family `TimeProvider` convention).
- **1.0.0**: stable SemVer contract, full snapshot coverage, and an optional `GrpcAssertions.Analyzers` package.

## Family compatibility

The seven assertion-family packages: `LogAssertions.TUnit`, `TimeAssertions.TUnit`, `SnapshotAssertions.TUnit`, `MathAssertions.TUnit`, `JsonAssertions.TUnit`, `SseAssertions.TUnit`, and `GrpcAssertions.TUnit`: release independently and target the same .NET TFM at any moment (LTS-anchored, multi-target during STS support windows; see the [TFM policy in CONVENTIONS.md](CONVENTIONS.md#tfm-policy) for the rotation schedule). **Mix versions freely.** Each package ships under SemVer with `EnablePackageValidation` strict-mode ApiCompat against its previous baseline, so binary breaks within a version line are caught at pack time.

For per-package release notes:
- [LogAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/LogAssertions.TUnit/blob/main/CHANGELOG.md)
- [TimeAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/TimeAssertions.TUnit/blob/main/CHANGELOG.md)
- [SnapshotAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/SnapshotAssertions.TUnit/blob/main/CHANGELOG.md)
- [MathAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/MathAssertions.TUnit/blob/main/CHANGELOG.md)
- [JsonAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/JsonAssertions.TUnit/blob/main/CHANGELOG.md)
- [SseAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/SseAssertions.TUnit/blob/main/CHANGELOG.md)
- [GrpcAssertions.TUnit CHANGELOG](https://github.com/JohnVerheij/GrpcAssertions.TUnit/blob/main/CHANGELOG.md)

## Pair with

- **[`LogAssertions.TUnit`](https://www.nuget.org/packages/LogAssertions.TUnit/)**: fluent log assertions over `Microsoft.Extensions.Logging.Testing.FakeLogCollector`.
- **[`TimeAssertions.TUnit`](https://www.nuget.org/packages/TimeAssertions.TUnit/)**: `TimeProvider`-aware time assertions and cross-cutting `.WithinTimeBudget(...)` chain methods.
- **[`SnapshotAssertions.TUnit`](https://www.nuget.org/packages/SnapshotAssertions.TUnit/)**: text-snapshot assertions for API-surface tests and similar deterministic-string scenarios. Coexists with Verify; covers the 80% case without coverage friction.
- **[`MathAssertions.TUnit`](https://www.nuget.org/packages/MathAssertions.TUnit/)**: tolerance-aware fluent assertions over numeric and geometric types (vectors, quaternions, matrices, planes, complex numbers, arrays).
- **[`JsonAssertions.TUnit`](https://www.nuget.org/packages/JsonAssertions.TUnit/)**: fluent JSON assertions over `System.Text.Json`, HTTP response bodies (including RFC 7807 ProblemDetails), and source-generated `JsonSerializerContext` registration.
- **[`SseAssertions.TUnit`](https://www.nuget.org/packages/SseAssertions.TUnit/)**: Server-Sent Events wire-format and stream assertions over HTTP response bodies, streams, and strings.

## Contributing

Issues and pull requests are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for the build, test, and snapshot-acceptance workflow, and [CONVENTIONS.md](CONVENTIONS.md) for the family-wide structure and policy. By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## License

[MIT](LICENSE). Takes a single runtime dependency on `Grpc.Core.Api` (Apache-2.0).
