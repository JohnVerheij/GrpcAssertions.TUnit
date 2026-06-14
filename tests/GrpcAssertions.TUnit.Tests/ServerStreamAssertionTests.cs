using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>Streams()</c> server-streaming assertion chain: the count narrowers
/// (<c>StreamsAtLeast</c>, <c>StreamsExactly</c>), the content narrower (<c>StreamContains</c>), the
/// <c>AndStreamItems</c> follow-on terminator, and the mid-stream fault diagnostic.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class ServerStreamAssertionTests
{
    private static readonly string[] Abc = ["a", "b", "c"];
    private static readonly string[] Ab = ["a", "b"];

    private static AsyncServerStreamingCall<string> ThreeTicks()
        => GrpcCallBuilder.ServerStreaming(Abc);

    [Test]
    public async Task Streams_StreamsAtLeast_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(call).Streams(ct).StreamsAtLeast(2);
    }

    [Test]
    public async Task Streams_StreamsAtLeast_BelowThreshold_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        var ex = await Assert.That(async () => await Assert.That(call).Streams(ct).StreamsAtLeast(5))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("fewer than the expected minimum of 5");
    }

    [Test]
    public async Task Streams_StreamsExactly_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(call).Streams(ct).StreamsExactly(3);
    }

    [Test]
    public async Task Streams_StreamsExactly_Mismatch_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        var ex = await Assert.That(async () => await Assert.That(call).Streams(ct).StreamsExactly(2))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("not the expected 2");
    }

    [Test]
    public async Task Streams_StreamContains_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(call).Streams(ct).StreamContains(static s => string.Equals(s, "b", StringComparison.Ordinal));
    }

    [Test]
    public async Task Streams_StreamContains_NoMatch_FailsNamingPredicate(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        var ex = await Assert.That(async () =>
                await Assert.That(call).Streams(ct).StreamContains(static s => string.Equals(s, "z", StringComparison.Ordinal)))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("no streamed response matched");
        await Assert.That(ex.Message).Contains("\"z\"");
    }

    [Test]
    public async Task Streams_AndStreamItems_RunsFollowOnAssertions(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(call).Streams(ct)
            .StreamsExactly(3)
            .AndStreamItems(async items =>
            {
                await Assert.That(items[0]).IsEqualTo("a");
                await Assert.That(items[2]).IsEqualTo("c");
            });
    }

    [Test]
    public async Task Streams_AndStreamItems_FailingFollowOn_Propagates(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(async () => await Assert.That(call).Streams(ct)
                .AndStreamItems(async items => await Assert.That(items[0]).IsEqualTo("wrong")))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Streams_FaultedStream_FailsWithPartialCountAndStatus(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = GrpcCallBuilder.ServerStreamingFaulted(StatusCode.Unavailable, Ab, "broke");
        var ex = await Assert.That(async () => await Assert.That(call).Streams(ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("the stream faulted after 2 item(s)");
        await Assert.That(ex.Message).Contains("Unavailable");
    }

    [Test]
    public async Task Streams_NullCall_FailsWithCallWasNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        AsyncServerStreamingCall<string> nullCall = null!;
        var ex = await Assert.That(async () => await Assert.That(nullCall).Streams(ct).StreamsAtLeast(0))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("the server-streaming call was null");
    }

    [Test]
    public async Task Streams_StreamsAtLeast_Negative_ThrowsArgumentOutOfRange(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(async () => await Assert.That(call).Streams(ct).StreamsAtLeast(-1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Streams_StreamsExactly_Negative_ThrowsArgumentOutOfRange(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        await Assert.That(async () => await Assert.That(call).Streams(ct).StreamsExactly(-1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Streams_StreamContains_NullPredicate_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        Func<string, bool> nullPredicate = null!;
        await Assert.That(async () => await Assert.That(call).Streams(ct).StreamContains(nullPredicate))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Streams_AndStreamItems_NullCallback_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var call = ThreeTicks();
        Func<IReadOnlyList<string>, Task> nullCallback = null!;
        await Assert.That(async () => await Assert.That(call).Streams(ct).AndStreamItems(nullCallback))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Streams_NullSource_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcServerStreamAssertionExtensions.Streams<string>(null!))
            .Throws<ArgumentNullException>();
    }
}
