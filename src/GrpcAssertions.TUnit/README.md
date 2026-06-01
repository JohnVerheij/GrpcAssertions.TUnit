# GrpcAssertions.TUnit

[![NuGet](https://img.shields.io/nuget/v/GrpcAssertions.TUnit.svg)](https://www.nuget.org/packages/GrpcAssertions.TUnit/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Scope:** Test projects only. Not intended for production code.

TUnit-native gRPC assertions for .NET tests. Fluent entry points over TUnit's `Assert.That(...)` pipeline for asserting on gRPC outcomes. AOT-compatible, trimmable, no runtime reflection in the assertion path.

> **v0.0.1 is a skeleton release** establishing the repository, package identifiers, and quality bar. It ships the `IsRpcException()` discriminator only; the full outcome-assertion surface (`ThrowsGrpcException` + the `StatusCode` shorthands, plus the `GrpcCallBuilder` test infrastructure) ships in v0.1.0.

## What ships

| Entry point | Receiver | Behaviour |
|---|---|---|
| `IsRpcException()` | `Exception` | Asserts the exception is a gRPC `RpcException`. The failure message names the actual exception type. |

## Install

```bash
dotnet add package GrpcAssertions.TUnit
```

**Requirements:** TUnit 1.47.0 or later, .NET 10. The framework-agnostic `GrpcAssertions` core and its `Grpc.Core.Api` dependency come transitively.

## Quick start

```csharp
[Test]
public async Task FailedCall_SurfacesRpcException()
{
    var caught = await Assert.That(() => client.GetAsync(request)).Throws<Exception>();

    await Assert.That(caught!).IsRpcException();
}
```

## License

MIT. Depends transitively on `Grpc.Core.Api` (Apache-2.0).
