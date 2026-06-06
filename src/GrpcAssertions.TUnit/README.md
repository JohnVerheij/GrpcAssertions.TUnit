# GrpcAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native gRPC assertions for .NET tests. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting on gRPC call outcomes. AOT-compatible, trimmable, no runtime reflection in the assertion path.

## What ships

Delegate assertions on `Assert.That(() => client.Method(...))`:

| Entry point | Behavior |
|---|---|
| `ThrowsGrpcException()` / `ThrowsGrpcException(StatusCode)` | Asserts the call throws an `RpcException` (optionally with a status). Returns a chain. |
| `DoesNotThrowGrpcException()` | Asserts the call completes without throwing an `RpcException`. |
| `IsRpcException()` (on a caught `Exception`) | Asserts the exception is a gRPC `RpcException`. |

Chain off `ThrowsGrpcException()`: 14 `StatusCode` shorthands (`IsUnavailable()`, `IsNotFound()`, and the rest), plus `WithDetail(string)` and `WithDetailContaining(string, StringComparison)`.

The framework-agnostic core (`GrpcAssertions`) also ships the `GrpcCallBuilder` test-double helper for building `AsyncUnaryCall<T>` fakes. `Success<T>(T)` infers `T` from its argument; `Faulted<T>(RpcException)` needs the explicit `T`. The [GitHub README](https://github.com/JohnVerheij/GrpcAssertions.TUnit#cookbook-common-patterns) has a before/after recipe for replacing hand-rolled `AsyncUnaryCall<T>` factories, and guidance on when *not* to migrate a test to `ThrowsGrpcException`.

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

**Requirements:** TUnit 1.50.0 or later, .NET 10. The framework-agnostic `GrpcAssertions` core and its `Grpc.Core.Api` dependency come transitively.

## Quick start

```csharp
await Assert.That(() => client.GetOrderAsync(request, ct))
    .ThrowsGrpcException(StatusCode.Unavailable)
    .WithDetailContaining("connection refused", StringComparison.Ordinal);

await Assert.That(() => client.CancelOrderAsync(request, ct))
    .DoesNotThrowGrpcException();
```

The full reference, including the `GrpcCallBuilder` test doubles and the complete shorthand list, is in the [GitHub README](https://github.com/JohnVerheij/GrpcAssertions.TUnit#readme).

## License

MIT. Depends transitively on `Grpc.Core.Api` (Apache-2.0).
