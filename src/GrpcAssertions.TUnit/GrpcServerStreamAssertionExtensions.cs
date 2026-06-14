using System;
using System.Threading;
using Grpc.Core;
using GrpcAssertions.TUnit;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// TUnit entry point for asserting on the responses of a gRPC server-streaming call. Extends the
/// value assertion source produced by <c>Assert.That(call)</c> where <c>call</c> is an
/// <see cref="AsyncServerStreamingCall{TResponse}"/>, returning a chain that reads the response
/// stream once and validates the accumulated expectations. The method lives in
/// <c>TUnit.Assertions.Extensions</c> so it is auto-imported alongside TUnit's own assertions.
/// </summary>
public static class GrpcServerStreamAssertionExtensions
{
    /// <summary>
    /// Begins a server-streaming assertion over the call's responses. Chain <c>StreamsAtLeast</c>,
    /// <c>StreamsExactly</c>, <c>StreamContains</c>, and <c>AndStreamItems</c> to narrow the
    /// assertion; the response stream is read once, when the assertion is awaited.
    /// </summary>
    /// <typeparam name="TResponse">The streamed response message type.</typeparam>
    /// <param name="source">The value assertion source carrying the server-streaming call.</param>
    /// <param name="cancellationToken">The token threaded into the response-stream read.</param>
    /// <returns>A <see cref="ServerStreamAssertion{TResponse}"/> for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static ServerStreamAssertion<TResponse> Streams<TResponse>(
        this IAssertionSource<AsyncServerStreamingCall<TResponse>> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Context.ExpressionBuilder.Append(".Streams()");
        return new ServerStreamAssertion<TResponse>(source.Context, cancellationToken);
    }
}
