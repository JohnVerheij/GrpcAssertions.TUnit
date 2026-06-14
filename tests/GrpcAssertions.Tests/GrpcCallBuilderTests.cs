using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    [Test]
    public async Task Faulted_WithTrailers_NullDetail_IsEmpty(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "k", "v" } };
        using var call = GrpcCallBuilder.Faulted<string>(StatusCode.Internal, null, trailers);

        var thrown = await Assert.That(async () => await call.ResponseAsync).Throws<RpcException>();
        await Assert.That(thrown!.Status.Detail).IsEqualTo(string.Empty);
        await Assert.That(thrown.Trailers.GetValue("k")).IsEqualTo("v");
    }

    // ---- server-streaming builders (0.3.0) ----

    private static readonly string[] Abc = ["a", "b", "c"];
    private static readonly string[] Ab = ["a", "b"];
    private static readonly string[] OneTwo = ["1", "2"];
    private static readonly string[] SingleA = ["a"];

    private static async Task<List<string>> DrainAsync(IAsyncStreamReader<string> reader, CancellationToken ct)
    {
        var items = new List<string>();
        while (await reader.MoveNext(ct))
        {
            items.Add(reader.Current);
        }

        return items;
    }

    [Test]
    public async Task ServerStreaming_YieldsAllResponsesThenEndsCleanly(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreaming(Abc);

        var items = await DrainAsync(call.ResponseStream, ct);
        await Assert.That(items).IsEquivalentTo(Abc);
        await Assert.That(call.GetStatus().StatusCode).IsEqualTo(StatusCode.OK);
        await Assert.That(call.GetTrailers().Count).IsEqualTo(0);
    }

    [Test]
    public async Task ServerStreaming_FromLazyEnumerable_Materializes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // A lazy (non-IReadOnlyList) sequence exercises the builder's ToList materialization path.
        using var call = GrpcCallBuilder.ServerStreaming(Enumerable.Range(1, 2).Select(i => i.ToString(CultureInfo.InvariantCulture)));

        var items = await DrainAsync(call.ResponseStream, ct);
        await Assert.That(items).IsEquivalentTo(OneTwo);
    }

    [Test]
    public async Task ServerStreaming_Empty_EndsImmediately(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreaming(Array.Empty<string>());

        await Assert.That(await call.ResponseStream.MoveNext(ct)).IsFalse();
    }

    [Test]
    public async Task ServerStreaming_CurrentBeforeMoveNext_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreaming(SingleA);

        await Assert.That(() => _ = call.ResponseStream.Current).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ServerStreaming_MoveNextWithCanceledToken_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreaming(SingleA);
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert.That(async () => await call.ResponseStream.MoveNext(canceled.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ServerStreaming_NullResponses_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcCallBuilder.ServerStreaming<string>(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ServerStreamingFaulted_YieldsResponsesThenThrows(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreamingFaulted(StatusCode.Unavailable, Ab, "stream broke");

        await Assert.That(await call.ResponseStream.MoveNext(ct)).IsTrue();
        await Assert.That(call.ResponseStream.Current).IsEqualTo("a");
        await Assert.That(await call.ResponseStream.MoveNext(ct)).IsTrue();

        var thrown = await Assert.That(async () => await call.ResponseStream.MoveNext(ct)).Throws<RpcException>();
        await Assert.That(thrown!.StatusCode).IsEqualTo(StatusCode.Unavailable);
        await Assert.That(thrown.Status.Detail).IsEqualTo("stream broke");
        await Assert.That(call.GetStatus().StatusCode).IsEqualTo(StatusCode.Unavailable);
    }

    [Test]
    public async Task ServerStreamingFaulted_DefaultDetailIsEmpty(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreamingFaulted(StatusCode.Internal, Array.Empty<string>());

        var thrown = await Assert.That(async () => await call.ResponseStream.MoveNext(ct)).Throws<RpcException>();
        await Assert.That(thrown!.Status.Detail).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ServerStreamingFaulted_NullResponses_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcCallBuilder.ServerStreamingFaulted<string>(StatusCode.Internal, null!))
            .Throws<ArgumentNullException>();
    }
}
