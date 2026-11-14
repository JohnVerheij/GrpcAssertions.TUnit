using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TUnit.Assertions.Exceptions;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>End-to-end tests for the <c>IsRpcException()</c> assertion, the v0.0.1 discriminator.</summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class RpcExceptionAssertionsTests
{
    [Test]
    public async Task IsRpcException_RpcException_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception exception = new RpcException(new Status(StatusCode.NotFound, "missing"));
        await Assert.That(exception).IsRpcException();
    }

    [Test]
    public async Task IsRpcException_OtherException_FailsNamingType(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception exception = new InvalidOperationException("boom");
        var ex = await Assert.That(async () =>
        {
            await Assert.That(exception).IsRpcException();
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("Expected to be a gRPC RpcException\nbut it was InvalidOperationException");
        await Assert.That(ex.Message).DoesNotContain("System.InvalidOperationException");
    }

    [Test]
    public async Task IsRpcException_Null_FailsInsteadOfThrowing(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception? nullException = null;

        // Called directly, as the core predicate is: null is not an RpcException.
        var result = RpcExceptionAssertions.IsRpcException(nullException!);

        await Assert.That(result.IsPassed).IsFalse();
        await Assert.That(result.Message).IsEqualTo("it was null");
    }

    [Test]
    public async Task IsRpcException_NullThroughTheAssertionChain_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception? nullException = null;
        var ex = await Assert.That(async () => await Assert.That(nullException).IsRpcException())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("Expected to be a gRPC RpcException\nbut");
    }

    [Test]
    public async Task IsRpcException_WrappedRpcException_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception wrapped = new InvalidOperationException("outer", new RpcException(new Status(StatusCode.Internal, "inner")));
        var ex = await Assert.That(async () => await Assert.That(wrapped).IsRpcException())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but it was InvalidOperationException");
    }
}
