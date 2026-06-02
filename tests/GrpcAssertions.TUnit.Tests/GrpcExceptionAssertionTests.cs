using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the delegate-based gRPC outcome assertions: <c>ThrowsGrpcException</c>,
/// the status shorthands, the detail refinements, and <c>DoesNotThrowGrpcException</c>.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class GrpcExceptionAssertionTests
{
    private static Func<Task> Faulting(StatusCode code, string detail)
        => () => Task.FromException(new RpcException(new Status(code, detail)));

    private static Func<Task> FaultingOther()
        => () => Task.FromException(new InvalidOperationException("boom"));

    private static Func<Task> Completing()
        => () => Task.CompletedTask;

    [Test]
    public async Task ThrowsGrpcException_RpcExceptionThrown_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Faulting(StatusCode.Unavailable, "down")).ThrowsGrpcException();
    }

    [Test]
    public async Task ThrowsGrpcException_NothingThrown_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Completing()).ThrowsGrpcException()).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("no exception was thrown");
    }

    [Test]
    public async Task ThrowsGrpcException_NonRpcExceptionThrown_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingOther()).ThrowsGrpcException()).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("InvalidOperationException");
    }

    [Test]
    public async Task ThrowsGrpcExceptionWithStatus_Matching_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Faulting(StatusCode.Unavailable, "down")).ThrowsGrpcException(StatusCode.Unavailable);
    }

    [Test]
    public async Task ThrowsGrpcException_WrongStatus_FailsNamingExpectedAndActual(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.Internal, "boom")).ThrowsGrpcException(StatusCode.NotFound))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("StatusCode Internal");
        await Assert.That(ex.Message).Contains("NotFound");
    }

    [Test]
    public async Task StatusShorthands_PassForMatchingStatus(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Faulting(StatusCode.OK, "")).ThrowsGrpcException().IsOk();
        await Assert.That(Faulting(StatusCode.Cancelled, "")).ThrowsGrpcException().IsCancelled();
        await Assert.That(Faulting(StatusCode.InvalidArgument, "")).ThrowsGrpcException().IsInvalidArgument();
        await Assert.That(Faulting(StatusCode.DeadlineExceeded, "")).ThrowsGrpcException().IsDeadlineExceeded();
        await Assert.That(Faulting(StatusCode.NotFound, "")).ThrowsGrpcException().IsNotFound();
        await Assert.That(Faulting(StatusCode.AlreadyExists, "")).ThrowsGrpcException().IsAlreadyExists();
        await Assert.That(Faulting(StatusCode.PermissionDenied, "")).ThrowsGrpcException().IsPermissionDenied();
        await Assert.That(Faulting(StatusCode.ResourceExhausted, "")).ThrowsGrpcException().IsResourceExhausted();
        await Assert.That(Faulting(StatusCode.FailedPrecondition, "")).ThrowsGrpcException().IsFailedPrecondition();
        await Assert.That(Faulting(StatusCode.Aborted, "")).ThrowsGrpcException().IsAborted();
        await Assert.That(Faulting(StatusCode.Unimplemented, "")).ThrowsGrpcException().IsUnimplemented();
        await Assert.That(Faulting(StatusCode.Internal, "")).ThrowsGrpcException().IsInternal();
        await Assert.That(Faulting(StatusCode.Unavailable, "")).ThrowsGrpcException().IsUnavailable();
        await Assert.That(Faulting(StatusCode.Unauthenticated, "")).ThrowsGrpcException().IsUnauthenticated();
    }

    [Test]
    public async Task StatusShorthand_Mismatch_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.Internal, "boom")).ThrowsGrpcException().IsUnavailable())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("StatusCode Internal");
    }

    [Test]
    public async Task WithDetail_ExactMatch_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Faulting(StatusCode.Unavailable, "connection refused"))
            .ThrowsGrpcException(StatusCode.Unavailable).WithDetail("connection refused");
    }

    [Test]
    public async Task WithDetail_Mismatch_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.Unavailable, "connection refused"))
                .ThrowsGrpcException(StatusCode.Unavailable).WithDetail("timeout"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("connection refused");
    }

    [Test]
    public async Task WithDetailContaining_SubstringPresent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Faulting(StatusCode.DeadlineExceeded, "request timeout exceeded"))
            .ThrowsGrpcException(StatusCode.DeadlineExceeded)
            .WithDetailContaining("timeout", StringComparison.Ordinal);
    }

    [Test]
    public async Task WithDetailContaining_SubstringAbsent_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.DeadlineExceeded, "request timeout exceeded"))
                .ThrowsGrpcException(StatusCode.DeadlineExceeded)
                .WithDetailContaining("connection", StringComparison.Ordinal))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("request timeout exceeded");
    }

    [Test]
    public async Task DoesNotThrowGrpcException_NoThrow_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(Completing()).DoesNotThrowGrpcException();
    }

    [Test]
    public async Task DoesNotThrowGrpcException_RpcExceptionThrown_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.Unavailable, "down")).DoesNotThrowGrpcException())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("StatusCode Unavailable");
    }

    [Test]
    public async Task DoesNotThrowGrpcException_OtherExceptionThrown_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingOther()).DoesNotThrowGrpcException()).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("InvalidOperationException");
    }

    [Test]
    public async Task ThrowsGrpcException_NullSource_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => { _ = GrpcDelegateAssertionExtensions.ThrowsGrpcException<object>(null!); })
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ThrowsGrpcExceptionWithStatus_NullSource_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => { _ = GrpcDelegateAssertionExtensions.ThrowsGrpcException<object>(null!, StatusCode.Internal); })
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DoesNotThrowGrpcException_NullSource_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => { _ = GrpcDelegateAssertionExtensions.DoesNotThrowGrpcException<object>(null!); })
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task StatusSpecifiedTwice_ThrowsInvalidOperation(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () =>
            await Assert.That(Completing()).ThrowsGrpcException(StatusCode.NotFound).IsUnavailable())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DetailSpecifiedTwice_ThrowsInvalidOperation(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () =>
            await Assert.That(Completing()).ThrowsGrpcException().WithDetail("a").WithDetail("b"))
            .Throws<InvalidOperationException>();
    }
}
