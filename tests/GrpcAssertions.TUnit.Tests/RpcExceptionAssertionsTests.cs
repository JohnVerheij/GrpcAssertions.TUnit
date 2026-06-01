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

        await Assert.That(ex!.Message).Contains("RpcException");
        await Assert.That(ex.Message).Contains("InvalidOperationException");
    }

    [Test]
    public async Task IsRpcException_Null_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Exception nullException = null!;
        await Assert.That(async () => await Task.Run(() => nullException.IsRpcException()))
            .Throws<ArgumentNullException>();
    }
}
