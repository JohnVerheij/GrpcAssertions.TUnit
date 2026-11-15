using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;

namespace GrpcAssertions.Tests;

/// <summary>Tests for the <see cref="GrpcOutcomeRendering"/> failure-message helpers.</summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class GrpcOutcomeRenderingTests
{
    [Test]
    public async Task Describe_RendersStatusCodeAndDetail(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var exception = new RpcException(new Status(StatusCode.Unavailable, "server down"));

        var rendered = GrpcOutcomeRendering.Describe(exception);

        await Assert.That(rendered).IsEqualTo("RpcException with StatusCode Unavailable, Detail \"server down\"");
    }

    [Test]
    public async Task Describe_Null_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => GrpcOutcomeRendering.Describe(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DescribeStatus_NullDetail_RendersEmptyQuotedDetail(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.NotFound, null);

        await Assert.That(rendered).IsEqualTo("StatusCode NotFound, Detail \"\"");
    }

    [Test]
    public async Task DescribeStatus_LongDetail_TruncatesWithEllipsis(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var longDetail = new string('x', GrpcOutcomeRendering.MaxDetailLength + 50);

        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, longDetail);

        await Assert.That(rendered).Contains(new string('x', GrpcOutcomeRendering.MaxDetailLength));
        await Assert.That(rendered).EndsWith("…\"");
        await Assert.That(rendered).DoesNotContain(new string('x', GrpcOutcomeRendering.MaxDetailLength + 1));
    }

    [Test]
    public async Task DescribeStatus_ShortDetail_NotTruncated(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Aborted, "short");

        await Assert.That(rendered).IsEqualTo("StatusCode Aborted, Detail \"short\"");
    }

    [Test]
    [Arguments(199, false)]
    [Arguments(200, false)]
    [Arguments(201, true)]
    public async Task DescribeStatus_AroundTheLimit_TruncatesOnlyAboveIt(int length, bool truncated, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, new string('x', length));

        var expected = truncated
            ? string.Concat(new string('x', GrpcOutcomeRendering.MaxDetailLength), "…")
            : new string('x', length);
        await Assert.That(rendered).IsEqualTo(string.Concat("StatusCode Internal, Detail \"", expected, "\""));
    }

    [Test]
    public async Task DescribeStatus_SurrogatePairStraddlingTheLimit_IsDroppedWhole(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The pair occupies units 199 and 200 (zero based), so a cut at 200 would keep only the high half.
        var detail = string.Concat(new string('x', GrpcOutcomeRendering.MaxDetailLength - 1), "\U0001F600", "tail");

        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, detail);

        await Assert.That(rendered).IsEqualTo(string.Concat(
            "StatusCode Internal, Detail \"", new string('x', GrpcOutcomeRendering.MaxDetailLength - 1), "…\""));
        await Assert.That(HasLoneSurrogate(rendered)).IsFalse();
    }

    [Test]
    public async Task DescribeStatus_SurrogatePairEndingAtTheLimit_IsKept(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var head = string.Concat(new string('x', GrpcOutcomeRendering.MaxDetailLength - 2), "\U0001F600");
        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, head + "tail");

        await Assert.That(rendered).IsEqualTo(string.Concat("StatusCode Internal, Detail \"", head, "…\""));
    }

    [Test]
    public async Task DescribeStatus_LoneHighSurrogateAtTheLimit_IsCutAsIs(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var detail = string.Concat(new string('x', GrpcOutcomeRendering.MaxDetailLength - 1), "\uD83D", "tail");

        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, detail);

        await Assert.That(rendered).Contains(string.Concat(new string('x', GrpcOutcomeRendering.MaxDetailLength - 1), "\uD83D…"));
    }

    [Test]
    public async Task DescribeStatus_LineBreaksInDetail_AreEscapedSoTheFragmentIsOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, "one\r\ntwo\nthree");

        await Assert.That(rendered).IsEqualTo("StatusCode Internal, Detail \"one\\r\\ntwo\\nthree\"");
    }

    [Test]
    public async Task DescribeStatus_LineBreaksInTruncatedDetail_AreEscapedToo(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var detail = string.Concat("a\nb", new string('x', GrpcOutcomeRendering.MaxDetailLength));

        var rendered = GrpcOutcomeRendering.DescribeStatus(StatusCode.Internal, detail);

        await Assert.That(rendered).StartsWith("StatusCode Internal, Detail \"a\\nb");
        await Assert.That(rendered.Contains('\n', StringComparison.Ordinal)).IsFalse();
        await Assert.That(rendered).EndsWith("…\"");
    }

    private static bool HasLoneSurrogate(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var isPairedHigh = char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]);
            var isPairedLow = char.IsLowSurrogate(value[i]) && i > 0 && char.IsHighSurrogate(value[i - 1]);
            if (char.IsSurrogate(value[i]) && !isPairedHigh && !isPairedLow)
            {
                return true;
            }
        }

        return false;
    }
}
