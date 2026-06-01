using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;

namespace GrpcAssertions.Tests;

/// <summary>Tests for the framework-agnostic <see cref="GrpcExceptions"/> predicates (v0.0.1).</summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class GrpcExceptionsTests
{
    [Test]
    public async Task IsRpcException_RpcException_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var exception = new RpcException(new Status(StatusCode.Unavailable, "server down"));
        await Assert.That(GrpcExceptions.IsRpcException(exception)).IsTrue();
    }

    [Test]
    public async Task IsRpcException_OtherException_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(GrpcExceptions.IsRpcException(new InvalidOperationException())).IsFalse();
    }

    [Test]
    public async Task IsRpcException_Null_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(GrpcExceptions.IsRpcException(null)).IsFalse();
    }
}
