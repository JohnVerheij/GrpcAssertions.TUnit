using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>
/// Real-use scenarios: the fakes and assertions exercised the way a consumer's suite calls them (hooks,
/// constructors, shared fixtures, parallel tests, the un-awaited call gotcha).
/// </summary>
[Category("Smoke")]
[Timeout(10_000)]
internal sealed class RealUseTests
{
    private static AsyncUnaryCall<string> s_sharedFaulted = null!;
    private static AsyncUnaryCall<string> s_sharedSuccess = null!;

    private readonly AsyncUnaryCall<string> _fromConstructor = GrpcCallBuilder.Faulted<string>(StatusCode.NotFound, "ctor");
    private AsyncUnaryCall<string> _fromHook = null!;

    [Before(Class)]
    public static void BuildSharedCalls(ClassHookContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        s_sharedFaulted = GrpcCallBuilder.Faulted<string>(StatusCode.Unavailable, "down", new Metadata { { "error-code", "E42" } });
        s_sharedSuccess = GrpcCallBuilder.Success("ok", new Metadata { { "h", "1" } }, new Metadata { { "t", "2" } });
    }

    [After(Class)]
    public static void DisposeSharedCalls(ClassHookContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        s_sharedFaulted.Dispose();
        s_sharedSuccess.Dispose();
    }

    [Before(Test)]
    public void BuildHookCall(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _fromHook = GrpcCallBuilder.Faulted<string>(StatusCode.Internal, "hook");
    }

    [After(Test)]
    public void DisposeHookCall(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _fromHook.Dispose();
    }

    [Test]
    [Repeat(5)]
    public async Task SharedFaultedCall_ReadByParallelTests_StaysConsistent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () => await s_sharedFaulted).ThrowsGrpcException(StatusCode.Unavailable)
            .WithDetail("down").WithTrailer("error-code", "E42");
        await Assert.That(s_sharedFaulted.GetTrailers()).IsSameReferenceAs(s_sharedFaulted.GetTrailers());
    }

    [Test]
    [Repeat(5)]
    public async Task SharedSuccessCall_ReadByParallelTests_StaysConsistent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(await s_sharedSuccess).IsEqualTo("ok");
        await Assert.That(s_sharedSuccess.GetTrailers().GetValue("t")).IsEqualTo("2");
        await Assert.That((await s_sharedSuccess.ResponseHeadersAsync).GetValue("h")).IsEqualTo("1");
    }

    [Test]
    public async Task ConstructorAndHookCalls_AreAssertable(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () => await _fromConstructor).ThrowsGrpcException().IsNotFound();
        await Assert.That(async () => await _fromHook).ThrowsGrpcException().IsInternal();
    }

    [Test]
    public async Task UnawaitedCall_ThrowsNothing_AndSaysSo(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(() => _fromConstructor).ThrowsGrpcException()).Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("no exception was thrown");
    }

    [Test]
    public async Task ResponseAsyncDelegate_IsAssertable(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => (Task)_fromConstructor.ResponseAsync).ThrowsGrpcException().IsNotFound();
        await Assert.That(async () => await s_sharedSuccess).DoesNotThrowGrpcException();
        await Assert.That(() => (Task)s_sharedSuccess.ResponseAsync).DoesNotThrowGrpcException();
    }

    [Test]
    public async Task DoesNotThrowGrpcException_OverFaultedCall_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(async () => await _fromHook).DoesNotThrowGrpcException()).Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("StatusCode Internal");
    }

    [Test]
    [Arguments(198)]
    [Arguments(199)]
    [Arguments(200)]
    [Arguments(201)]
    [Arguments(5000)]
    public async Task ActualDetail_AroundTheLimit_RendersWithoutThrowing(int length, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var detail = new string('x', length);
        var ex = await Assert.That(async () =>
            await Assert.That(async () => await GrpcCallBuilder.Faulted<string>(StatusCode.Internal, detail))
                .ThrowsGrpcException().WithDetail("other")).Throws<AssertionException>();
        await Assert.That(ex!.Message.Contains('…', StringComparison.Ordinal)).IsEqualTo(length > 200);
    }

    [Test]
    public async Task DuplicateAndMixedCaseTrailers_Match(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "k", "1" }, { "K", "2" }, { "bin-bin", new byte[] { 1, 2 } } };
        await Assert.That(async () => await GrpcCallBuilder.Faulted<string>(StatusCode.Aborted, "x", trailers))
            .ThrowsGrpcException().WithTrailer("k", "2").WithTrailer("K", "1").WithTrailer("bin-bin", new byte[] { 1, 2 });
    }

    [Test]
    public async Task StreamsTwiceOnOneCall_SecondReadSeesEmptyStream(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var call = GrpcCallBuilder.ServerStreaming(["a", "b"]);
        await Assert.That(call).Streams(ct).StreamsExactly(2);
        await Assert.That(call).Streams(ct).StreamsExactly(0);
    }

    [Test]
    public async Task Streams_CanceledToken_PropagatesOperationCanceled(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var call = GrpcCallBuilder.ServerStreaming(["a"]);
        await Assert.That(async () => await Assert.That(call).Streams(cts.Token).StreamsAtLeast(1))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Streams_CallbackFailure_SurfacesItsOwnMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var call = GrpcCallBuilder.ServerStreaming(["a"]);
        await Assert.That(async () => await Assert.That(call).Streams(ct)
            .AndStreamItems(async items => await Assert.That(items.Count).IsEqualTo(2))).Throws<AssertionException>();
    }

    [Test]
    public async Task ParallelFakeStreams_FromOneBuilder_AreIndependent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            var call = GrpcCallBuilder.ServerStreaming(Enumerable.Range(0, i).ToList());
            await Assert.That(call).Streams(ct).StreamsExactly(i);
        });
        await Task.WhenAll(tasks);
    }
}
