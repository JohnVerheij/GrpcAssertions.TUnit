# GrpcAssertions

[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.svg)](https://www.nuget.org/packages/GrpcAssertions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

Framework-agnostic core for the GrpcAssertions package family. Provides predicates over gRPC exceptions (the `Grpc.Core.Api` `RpcException` / `StatusCode` / `Status` types). The TUnit-native fluent assertion entry points ship in the adapter package [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/).

> **Most users want [`GrpcAssertions.TUnit`](https://www.nuget.org/packages/GrpcAssertions.TUnit/), not this package directly.** This package only ships the framework-agnostic core; the adapter package adds the assertion entry points your test framework expects.

## What's in this package

> **v0.0.1 is a skeleton release** that establishes the repository, package identifiers, and quality bar. The full gRPC outcome-assertion surface (the `GrpcCallBuilder` test infrastructure plus the `ThrowsGrpcException` verbs and `StatusCode` shorthands) ships in v0.1.0.

- **`GrpcExceptions`**: framework-agnostic predicates over gRPC exceptions. `IsRpcException(Exception?)` reports whether an exception is a gRPC `RpcException` (null and non-`RpcException` types return `false`).

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

The core (`GrpcAssertions`) comes transitively; install it directly only when authoring a non-TUnit adapter for the assertion family.

## License

MIT throughout. Takes a single runtime dependency on `Grpc.Core.Api` (Apache-2.0), the package that defines the gRPC `RpcException` / `StatusCode` / `Status` types the assertions are about.
