using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Core;

namespace Smoke.Consumer;

/// <summary>
/// External-consumer smoke test that verifies the just-packed GrpcAssertions.TUnit NuGet package
/// can be consumed from a deliberately-different namespace (<c>Smoke.Consumer</c>) without leaking
/// into GrpcAssertions.TUnit's internals. Compiles + runs against the local-feed version pinned in
/// <c>NuGet.config</c>, never the in-repo ProjectReference. This is the last CI step before release
/// and the canary that proves the packed nupkg is a usable consumer artifact (including the
/// transitive Grpc.Core.Api dependency).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class SmokeTest
{
    [Test]
    public async Task ConsumesGrpcExceptionsFromCore(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Exception exception = new RpcException(new Status(StatusCode.Unavailable, "down"));

        await Assert.That(GrpcExceptions.IsRpcException(exception)).IsTrue();
    }

    [Test]
    public async Task ConsumesIsRpcExceptionFromAdapter(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Exception exception = new RpcException(new Status(StatusCode.NotFound, "missing"));

        await Assert.That(exception).IsRpcException();
    }
}
