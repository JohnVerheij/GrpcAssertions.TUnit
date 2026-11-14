using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Extensions;

namespace GrpcAssertions.TUnit.Tests;

/// <summary>
/// Tests that every clause of a failure message stays on one line: a carriage return or line feed in a
/// thrown exception's message, an expected trailer key or value, or an actual trailer value is rendered as an
/// escape, never as a raw line break.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class FailureMessageLineBreakTests
{
    private const string DisposedMessage = "Cannot access a disposed object.\r\nObject name: 'channel'.";
    private const string DisposedEscaped = "Cannot access a disposed object.\\r\\nObject name: 'channel'.";

    [Test]
    public async Task ThrowsGrpcException_OtherExceptionWithLineBreaks_RendersOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Func<Task> faulting = () => Task.FromException(new ObjectDisposedException("channel", DisposedMessage));
        var ex = await Assert.That(async () => await Assert.That(faulting).ThrowsGrpcException())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("it threw ObjectDisposedException: Cannot access a disposed object.\\r\\n");
        await Assert.That(ex.Message).DoesNotContain("object.\r\nObject name");
    }

    [Test]
    public async Task DoesNotThrowGrpcException_OtherExceptionWithLineBreaks_RendersOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Func<Task> faulting = () => Task.FromException(new InvalidOperationException("first\r\nsecond\nthird"));
        var ex = await Assert.That(async () => await Assert.That(faulting).DoesNotThrowGrpcException())
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("it threw InvalidOperationException: first\\r\\nsecond\\nthird");
        await Assert.That(ex.Message).DoesNotContain("first\r\nsecond");
    }

    [Test]
    public async Task Streams_FactoryThrowsWithLineBreaks_RendersOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Func<AsyncServerStreamingCall<string>> factory = () => throw new ObjectDisposedException("channel", DisposedMessage);
        var ex = await Assert.That(async () => await Assert.That(factory).Streams(ct).StreamsAtLeast(0))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("it threw ObjectDisposedException: " + DisposedEscaped);
        await Assert.That(ex.Message).DoesNotContain("object.\r\nObject name");
    }

    [Test]
    public async Task WithTrailer_ExpectedKeyAndValueWithLineBreaks_RenderOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Func<Task> faulting = () => Task.FromException(new RpcException(new Status(StatusCode.Internal, "boom")));
        var ex = await Assert.That(async () =>
            await Assert.That(faulting).ThrowsGrpcException().WithTrailer("key\r\nx", "val\r\ny"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("with trailer \"key\\r\\nx\" = \"val\\r\\ny\"");
        await Assert.That(ex.Message).DoesNotContain("key\r\nx");
        await Assert.That(ex.Message).DoesNotContain("val\r\ny");
    }

    [Test]
    public async Task WithTrailer_ActualTextValueWithLineBreaks_RendersOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trailers = new Metadata { { "note", "line one\r\nline two" } };
        Func<Task> faulting = () => Task.FromException(new RpcException(new Status(StatusCode.Internal, "boom"), trailers));
        var ex = await Assert.That(async () =>
            await Assert.That(faulting).ThrowsGrpcException().WithTrailer("note", "other"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("trailers: note=\"line one\\r\\nline two\"");
        await Assert.That(ex.Message).DoesNotContain("line one\r\nline two");
    }
}
