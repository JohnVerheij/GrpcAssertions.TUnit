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

    private static Func<Task> FaultingWithTrailers(StatusCode code, string detail, Metadata trailers)
        => () => Task.FromException(new RpcException(new Status(code, detail), trailers));

    [Test]
    public async Task WithTrailer_TextMatch_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "error-code", "E42" } };
        await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
            .ThrowsGrpcException().WithTrailer("error-code", "E42");
    }

    [Test]
    public async Task WithTrailer_KeyMatchIsCaseInsensitive(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // gRPC lowercases metadata keys, so the expectation key may use any casing and still match.
        var trailers = new Metadata { { "error-code", "E42" } };
        await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
            .ThrowsGrpcException().WithTrailer("Error-Code", "E42");
    }

    [Test]
    public async Task WithTrailer_TextMismatch_FailsRenderingTrailers(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "error-code", "E42" } };
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
                .ThrowsGrpcException().WithTrailer("error-code", "WRONG"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("trailers: error-code=\"E42\"");
    }

    [Test]
    public async Task WithTrailer_MissingKey_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "present", "x" } };
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
                .ThrowsGrpcException().WithTrailer("absent", "x"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("present=");
    }

    [Test]
    public async Task WithTrailer_BinaryMatch_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var payload = new byte[] { 1, 2, 3 };
        var trailers = new Metadata { { "blob-bin", payload } };
        await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
            .ThrowsGrpcException().WithTrailer("blob-bin", payload);
    }

    [Test]
    public async Task WithTrailer_BinaryEntryNotMatchedByTextOverload_AndRendersBinary(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The text overload must not match (nor throw on reading the value of) a binary entry, and the
        // failure rendering must describe it as binary rather than reading its value as a string.
        var trailers = new Metadata { { "blob-bin", new byte[] { 9, 9 } } };
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
                .ThrowsGrpcException().WithTrailer("blob-bin", "anything"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("(binary, 2 bytes)");
    }

    [Test]
    public async Task WithTrailer_BinaryMismatch_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "blob-bin", new byte[] { 1, 2 } } };
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
                .ThrowsGrpcException().WithTrailer("blob-bin", new byte[] { 9, 9 }))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("blob-bin=(binary, 2 bytes)");
    }

    [Test]
    public async Task WithTrailer_NoTrailers_FailsRenderingNone(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That(Faulting(StatusCode.Internal, "boom")).ThrowsGrpcException().WithTrailer("k", "v"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("trailers: (none)");
    }

    [Test]
    public async Task WithTrailer_BinaryExpectationVsTextEntry_FailsRenderingAllTrailers(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Two trailers (so the failure renders them comma-separated), and a binary expectation against
        // a text entry: a text entry is never matched as binary, and the text entry's value is rendered.
        var trailers = new Metadata { { "code", "E1" }, { "note", "n" } };
        var ex = await Assert.That(async () =>
            await Assert.That(FaultingWithTrailers(StatusCode.Internal, "boom", trailers))
                .ThrowsGrpcException().WithTrailer("code", new byte[] { 5 }))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("code=\"E1\", note=\"n\"");
    }

    [Test]
    public async Task WithTrailer_AfterStatusAndDetail_AllChecked(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "error-code", "E1" } };
        await Assert.That(FaultingWithTrailers(StatusCode.NotFound, "missing", trailers))
            .ThrowsGrpcException(StatusCode.NotFound)
            .WithDetail("missing")
            .WithTrailer("error-code", "E1");
    }

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
