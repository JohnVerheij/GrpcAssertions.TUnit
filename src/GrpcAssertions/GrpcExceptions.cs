using System;
using Grpc.Core;

namespace GrpcAssertions;

/// <summary>
/// Framework-agnostic predicates over gRPC exceptions. The v0.0.1 surface is intentionally
/// minimal: it establishes the package, the <c>Grpc.Core.Api</c> dependency, and the quality bar.
/// The full gRPC outcome-assertion surface (the <c>GrpcCallBuilder</c> test infrastructure plus the
/// <c>ThrowsGrpcException</c> verbs and <c>StatusCode</c> shorthands) ships in v0.1.0.
/// </summary>
public static class GrpcExceptions
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="exception"/> is a gRPC
    /// <see cref="RpcException"/> (the exception type a failed gRPC call surfaces). Returns
    /// <see langword="false"/> for <see langword="null"/> and for any other exception type.
    /// </summary>
    /// <param name="exception">The exception to test. May be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="exception"/> is an <see cref="RpcException"/>.</returns>
    public static bool IsRpcException(Exception? exception) => exception is RpcException;
}
