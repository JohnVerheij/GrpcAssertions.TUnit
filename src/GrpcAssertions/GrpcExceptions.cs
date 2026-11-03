using System;
using Grpc.Core;

namespace GrpcAssertions;

/// <summary>
/// Framework-agnostic predicates over gRPC exceptions. Test-framework adapters build their
/// exception assertions on these, and tests can call them directly.
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
