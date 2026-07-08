# GrpcAssertions

[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.svg)](https://www.nuget.org/packages/GrpcAssertions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

> Part of the **[DotNetAssertions](https://dotnetassertions.dev)** family. This is the framework-agnostic core; the TUnit assertions live in the matching `.TUnit` package.

Framework-agnostic core for the GrpcAssertions package family. The TUnit-native fluent assertion entry points ship in the adapter package [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/).

> **Most users want [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/), not this package directly.** Install this core directly only when authoring a non-TUnit adapter or when you only need the test-double builder and predicates.

## What's in this package

- **`GrpcCallBuilder`**: builds gRPC client test doubles, replacing the multi-parameter constructor every hand-rolled fake repeats. `AsyncUnaryCall<T>` via `Success<T>(T)` (infers `T` from its argument), `Faulted<T>(RpcException)` and `Faulted<T>(StatusCode, string?)` (need the explicit `T`); `AsyncServerStreamingCall<T>` via `ServerStreaming<T>(IEnumerable<T>)` and `ServerStreamingFaulted<T>(StatusCode, IEnumerable<T>, string?)` *(v0.3.0+)*. The [GitHub README](https://github.com/JohnVerheij/GrpcAssertions.TUnit#cookbook-common-patterns) has a before/after recipe for replacing hand-rolled factories.
- **`GrpcOutcomeRendering`**: renders a gRPC outcome (`StatusCode` plus truncated `Status.Detail`) for failure messages, shared so consumer-authored gRPC assertions produce identical diagnostics.
- **`GrpcExceptions`**: `IsRpcException(Exception?)` reports whether an exception is a gRPC `RpcException` (null and non-`RpcException` types return `false`).

All over the single `Grpc.Core.Api` dependency (`RpcException` / `StatusCode` / `Status`).

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

The core (`GrpcAssertions`) comes transitively; install it directly only when authoring a non-TUnit adapter for the assertion family.

## License

MIT throughout. Takes a single runtime dependency on `Grpc.Core.Api` (Apache-2.0), the package that defines the gRPC `RpcException` / `StatusCode` / `Status` types the assertions are about.
