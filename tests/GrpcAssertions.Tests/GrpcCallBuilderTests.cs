using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;

namespace GrpcAssertions.Tests;

/// <summary>Tests for the <see cref="GrpcCallBuilder"/> test-double helpers.</summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class GrpcCallBuilderTests
{
    [Test]
    public async Task Success_CompletesWithResponseOkStatusAndEmptyTrailers(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.Success("reply");

        await Assert.That(await call.ResponseAsync).IsEqualTo("reply");
        await Assert.That(call.GetStatus().StatusCode).IsEqualTo(StatusCode.OK);
        await Assert.That(call.GetTrailers().Count).IsEqualTo(0);
        await Assert.That((await call.ResponseHeadersAsync).Count).IsEqualTo(0);
    }

    [Test]
    public async Task Success_NullResponse_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcCallBuilder.Success<string>(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Faulted_FromException_SurfacesExceptionStatusAndTrailers(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "error-code", "BOOM" } };
        var exception = new RpcException(new Status(StatusCode.Unavailable, "server down"), trailers);
        using var call = GrpcCallBuilder.Faulted<string>(exception);

        var thrown = await Assert.That(async () => await call.ResponseAsync).Throws<RpcException>();
        await Assert.That(thrown!.StatusCode).IsEqualTo(StatusCode.Unavailable);
        await Assert.That(call.GetStatus().Detail).IsEqualTo("server down");
        await Assert.That(call.GetTrailers().GetValue("error-code")).IsEqualTo("BOOM");
    }

    [Test]
    public async Task Faulted_NullException_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcCallBuilder.Faulted<string>((RpcException)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Faulted_FromStatusCodeAndDetail_FaultsWithThatStatus(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.Faulted<string>(StatusCode.NotFound, "missing");

        var thrown = await Assert.That(async () => await call.ResponseAsync).Throws<RpcException>();
        await Assert.That(thrown!.StatusCode).IsEqualTo(StatusCode.NotFound);
        await Assert.That(thrown.Status.Detail).IsEqualTo("missing");
    }

    [Test]
    public async Task Faulted_FromStatusCode_DefaultDetailIsEmpty(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.Faulted<string>(StatusCode.Internal);

        var thrown = await Assert.That(async () => await call.ResponseAsync).Throws<RpcException>();
        await Assert.That(thrown!.Status.Detail).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Success_WithHeadersAndTrailers_SurfacesThem(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var headers = new Metadata { { "x-header", "h" } };
        var trailers = new Metadata { { "x-trailer", "t" } };
        using var call = GrpcCallBuilder.Success("reply", headers, trailers);

        await Assert.That(await call.ResponseAsync).IsEqualTo("reply");
        await Assert.That((await call.ResponseHeadersAsync).GetValue("x-header")).IsEqualTo("h");
        await Assert.That(call.GetTrailers().GetValue("x-trailer")).IsEqualTo("t");
    }

    [Test]
    public async Task Success_TrailersAccessor_ReturnsStableInstance(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.Success("reply");

        // A real client returns the same trailers instance on every call; the builder must too.
        await Assert.That(ReferenceEquals(call.GetTrailers(), call.GetTrailers())).IsTrue();
    }

    [Test]
    public async Task Faulted_WithTrailers_AttachesToException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "retry-after", "30" } };
        using var call = GrpcCallBuilder.Faulted<string>(StatusCode.ResourceExhausted, "slow down", trailers);

        var thrown = await Assert.That(async () => await call.ResponseAsync).Throws<RpcException>();
        await Assert.That(thrown!.StatusCode).IsEqualTo(StatusCode.ResourceExhausted);
        await Assert.That(thrown.Trailers.GetValue("retry-after")).IsEqualTo("30");
    }

    [Test]
    public async Task Faulted_WithTrailers_NullTrailers_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcCallBuilder.Faulted<string>(StatusCode.Internal, "x", (Metadata)null!))
            .Throws<ArgumentNullException>();
    }
}
