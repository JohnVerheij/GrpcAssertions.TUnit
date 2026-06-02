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
}
